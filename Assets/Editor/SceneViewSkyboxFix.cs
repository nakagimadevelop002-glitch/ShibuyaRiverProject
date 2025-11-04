using UnityEditor;

[InitializeOnLoad]
public static class SceneViewSkyboxFix
{
    static SceneViewSkyboxFix()
    {
        EditorApplication.delayCall += EnableSkyboxInAllSceneViews;
    }

    private static void EnableSkyboxInAllSceneViews()
    {
        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            sceneView.sceneViewState.showSkybox = true;
            sceneView.Repaint();
        }
    }
}
