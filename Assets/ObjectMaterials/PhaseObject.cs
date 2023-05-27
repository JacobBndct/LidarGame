using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace LidarMaterial
{
    public class PhaseObject : ObjectMaterial
    {
        Material material;

        void Start()
        {
            material = GetComponent<Renderer>().material;
            material.color = new Color(material.color.r, material.color.g, material.color.b, energy / 10000f);
        }

        public override void OnHit(int addedEnergy)
        {
            base.OnHit(addedEnergy);
            material.color = new Color(material.color.r, material.color.g, material.color.b, Mathf.Clamp(energy / 10000f, 0f, 1f));
        }

        protected override void EnergyLoss()
        {
            if (energy <= 0)
            {
                return;
            }
            else
            {
                energy--;
                material.color = new Color(material.color.r, material.color.g, material.color.b, Mathf.Clamp(energy / 10000f, 0f, 1f));
            }
        }
    }
}
