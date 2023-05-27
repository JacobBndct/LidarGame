using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace LidarMaterial
{
    public class GlowObject : ObjectMaterial
    {
        Material material;
        Light lightSource;
        // Start is called before the first frame update
        void Start()
        {
            InitializeLight();
        }

        public override void OnHit(int addedEnergy)
        {
            base.OnHit(addedEnergy);
            UpdateLightIntensity();
        }

        //TODO: this object should emit energy to nearby objects
        protected override void EnergyLoss()
        {
            if (energy <= 0)
            {
                TurnOffLight();
            }
            else
            {
                energy--;
                UpdateLightIntensity();
            }
        }

        private void InitializeLight()
        {
            lightSource = this.AddComponent<Light>();
            lightSource.range = 0;
            lightSource.intensity = 0;

            material = GetComponent<Renderer>().material;
            material.EnableKeyword("_EMISSION");
        }

        private void UpdateLightIntensity()
        {
            float lightIntensity = energy / 1000f;
            lightSource.range = lightIntensity;
            lightSource.intensity = lightIntensity;

            Color colour = material.color;
            material.SetColor("_EmissionColor", colour * lightIntensity);
        }

        private void TurnOffLight()
        {
            lightSource.range = 0;
            lightSource.intensity = 0;

            Color colour = material.color;
            material.SetColor("_EmissionColor", colour * 0f);
        }
    }
}