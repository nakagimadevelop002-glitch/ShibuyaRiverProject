using UnityEngine;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// YOLOv8 ONNXモデルで人物検知を行うコンポーネント
/// CameraInputから画像を受け取り、検知した人数をカウント
/// </summary>
public class PersonDetector : MonoBehaviour
{
    [Header("モデル設定")]
    [Tooltip("YOLOv8 ONNXモデルファイル")]
    public string modelPath = "yolov8n.onnx";

    [Header("入力画像サイズ")]
    [Tooltip("入力画像のサイズ（YOLOv8は通常640x640）")]
    public int inputSize = 640;

    [Header("検出信頼度閾値")]
    [Tooltip("検出信頼度の閾値")]
    [Range(0f, 1f)]
    public float confidenceThreshold = 0.5f;

    [Header("検知間隔")]
    [Tooltip("推論を実行するフレーム間隔（1=毎フレーム、30=1秒ごと）")]
    [Range(1, 300)]
    public int inferenceInterval = 30;

    [Header("検出結果の平滑化")]
    [Tooltip("検出結果を平滑化する")]
    public bool enableSmoothing = true;

    [Header("平滑化フレーム数")]
    [Tooltip("平滑化に使用する過去フレーム数")]
    [Range(3, 20)]
    public int smoothingFrames = 5;

    [Header("カメラ入力参照")]
    [Tooltip("CameraInputコンポーネント")]
    public CameraInput cameraInput;

    [Header("検出人数（出力）")]
    [Tooltip("現在検出されている人数")]
    public int detectedPeopleCount = 0;

    [Header("生の検出人数（出力）")]
    [Tooltip("平滑化前の生の検出人数")]
    public int rawDetectedCount = 0;

    private InferenceSession session;
    private bool isModelLoaded = false;
    private int frameCounter = 0;
    private Queue<int> detectionHistory = new Queue<int>();

    void Start()
    {
        // CameraInputの自動検索
        if (cameraInput == null)
        {
            cameraInput = FindObjectOfType<CameraInput>();
        }

        LoadModel();
    }

    void LoadModel()
    {
        try
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, modelPath);

            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError($"PersonDetector: モデルファイルが見つかりません: {fullPath}");
                return;
            }

            // ONNX Runtimeのセッションを作成
            SessionOptions options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            session = new InferenceSession(fullPath, options);
            isModelLoaded = true;

            Debug.Log($"PersonDetector: モデル読み込み成功: {modelPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PersonDetector: モデル読み込みエラー: {e.Message}");
        }
    }

    void Update()
    {
        if (!isModelLoaded || cameraInput == null || !cameraInput.IsInitialized)
            return;

        frameCounter++;

        // 指定フレーム間隔で推論実行
        if (frameCounter >= inferenceInterval)
        {
            frameCounter = 0;

            if (cameraInput.DidUpdateThisFrame())
            {
                RunInference();
            }
        }
    }

    void RunInference()
    {
        try
        {
            Texture2D frame = cameraInput.GetCurrentFrame();
            if (frame == null) return;

            // 前処理: Texture2D → float配列に変換してリサイズ
            float[] inputData = PreprocessImage(frame);

            // テンソル作成
            var inputTensor = new DenseTensor<float>(inputData, new[] { 1, 3, inputSize, inputSize });

            // 推論実行
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", inputTensor)
            };

            using (var results = session.Run(inputs))
            {
                // YOLOv8の出力: [1, 84, 8400]
                // 84 = 4(bbox) + 80(classes)
                var output = results.First().AsEnumerable<float>().ToArray();

                // 後処理: 人物クラス(class_id=0)の検出数をカウント
                int rawCount = PostprocessOutput(output);
                rawDetectedCount = rawCount;

                // 平滑化処理
                if (enableSmoothing)
                {
                    detectedPeopleCount = ApplySmoothing(rawCount);
                }
                else
                {
                    detectedPeopleCount = rawCount;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PersonDetector: 推論エラー: {e.Message}");
        }
    }

    float[] PreprocessImage(Texture2D texture)
    {
        // リサイズ用のRenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(inputSize, inputSize, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(texture, rt);

        // RenderTextureから読み取り
        RenderTexture.active = rt;
        Texture2D resized = new Texture2D(inputSize, inputSize, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, inputSize, inputSize), 0, 0);
        resized.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // RGB → float配列に変換 (正規化: 0-255 → 0-1)
        Color[] pixels = resized.GetPixels();
        float[] inputData = new float[1 * 3 * inputSize * inputSize];

        for (int i = 0; i < pixels.Length; i++)
        {
            // CHW形式に変換 (Channels, Height, Width)
            inputData[i] = pixels[i].r;                          // R channel
            inputData[inputSize * inputSize + i] = pixels[i].g;  // G channel
            inputData[inputSize * inputSize * 2 + i] = pixels[i].b; // B channel
        }

        Destroy(resized);
        return inputData;
    }

    int PostprocessOutput(float[] output)
    {
        // YOLOv8出力形式: [1, 84, 8400]
        // output配列は [84 * 8400] = 705600要素
        int numDetections = 8400;
        //int numClasses = 80;
        int peopleCount = 0;

        for (int i = 0; i < numDetections; i++)
        {
            // 各検出のデータ位置
            // bbox: output[i], output[8400+i], output[16800+i], output[25200+i]
            // class scores: output[33600+i] ~ output[33600+79*8400+i]

            // 'person'クラスはCOCOデータセットでclass_id=0
            int personScoreIndex = 4 * numDetections + i; // 33600 + i
            float personScore = output[personScoreIndex];

            if (personScore >= confidenceThreshold)
            {
                peopleCount++;
            }
        }

        return peopleCount;
    }

    int ApplySmoothing(int currentCount)
    {
        // 履歴に追加
        detectionHistory.Enqueue(currentCount);

        // 指定フレーム数を超えたら古いデータを削除
        while (detectionHistory.Count > smoothingFrames)
        {
            detectionHistory.Dequeue();
        }

        // 移動平均を計算
        float average = 0f;
        foreach (int count in detectionHistory)
        {
            average += count;
        }
        average /= detectionHistory.Count;

        // 四捨五入して整数に変換
        return Mathf.RoundToInt(average);
    }

    void OnDestroy()
    {
        session?.Dispose();
    }
}
