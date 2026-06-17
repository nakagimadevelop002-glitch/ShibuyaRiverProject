using UnityEngine;
using System.Collections;

public class FishSelectScript : MonoBehaviour {
    public GameObject[] fishes;
    public FreshwaterfishCameraScript fishCamera;

	
    public void SelectFish(int fishNum)
    {
        foreach(GameObject aFish in fishes)
        {
             aFish.GetComponent<FishUserController>().enabled= false;
        }
        fishes[fishNum].GetComponent<FishUserController>().enabled = true;
        fishCamera.TargetSet(fishes[fishNum]);

    }
}
