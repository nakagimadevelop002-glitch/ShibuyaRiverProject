using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NekoLegends
{
    public class DemoFurHairAndFiber : DemoScenes
    {
        [Space]
        [SerializeField] private DemoCameraController _cameraController;

        [SerializeField] private GameObject PointLight, ExamplesContainer, GroundsContainer, PerformanceContainer;
        [SerializeField] private Button ChangeModelBtn, ChangeGroundBtn, TogglePointLightBtn, TestPerformanceBtn;

        [SerializeField] private List<GameObject> Models, Grounds, Performances;

        private int currentModelIndex = -1, currentGroundIndex = -1, currentPerformancesIndex = -1;
        

        protected override void Start()
        {
            base.Start();
            NextModel(); //starts at -1, so basically index 0 first one on start
            NextGround();
        }

        protected override void OnEnable()
        {
            if (ChangeModelBtn)
                ChangeModelBtn.onClick.AddListener(NextModel); // Register the new button action
            ChangeGroundBtn.onClick.AddListener(NextGround);
            TogglePointLightBtn.onClick.AddListener(TogglePointLight);
            TestPerformanceBtn.onClick.AddListener(HandleTestPerformanceBtn);

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (ChangeModelBtn)
                ChangeModelBtn.onClick.RemoveListener(NextModel); // Remember to remove the listener to prevent memory leaks

            base.OnDisable();
        }

        private void NextModel()
        {
            ToggleIsPerformanceVisibles(false);
            currentModelIndex = (currentModelIndex + 1) % Models.Count;

            foreach (var item in Models)
            {
                item.SetActive(false);
            }
            Models[currentModelIndex].SetActive(true);

            SetDescriptionText(Models[currentModelIndex].name);
        }

        private void NextGround()
        {
            ToggleIsPerformanceVisibles(false);
            currentGroundIndex = (currentGroundIndex + 1) % Grounds.Count;

            foreach (var item in Grounds)
            {
                item.SetActive(false);
            }
            Grounds[currentGroundIndex].SetActive(true);
            SetDescriptionText(Grounds[currentGroundIndex].name);
        }

        private void TogglePointLight()
        {
            PointLight.SetActive(!PointLight.activeSelf);
            SetDescriptionText("Point Light:" + PointLight.activeSelf);
        }

        private void HandleTestPerformanceBtn()
        {
            ToggleIsPerformanceVisibles(true);

            currentPerformancesIndex = (currentPerformancesIndex + 1) % Performances.Count;

            foreach (var item in Performances)
            {
                item.SetActive(false);
            }
            Performances[currentPerformancesIndex].SetActive(true);
            SetDescriptionText(Performances[currentPerformancesIndex].name);

        }

        private void ToggleIsPerformanceVisibles(bool isPerformance)
        {
            ExamplesContainer.SetActive(!isPerformance);
            GroundsContainer.SetActive(!isPerformance);
            PerformanceContainer.SetActive(isPerformance);
        }

    }
}
