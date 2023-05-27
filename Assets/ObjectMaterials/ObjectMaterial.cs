using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LidarMaterial
{
    public abstract class ObjectMaterial : MonoBehaviour
    {
        protected int energy;
        protected int energyLossRate;

        protected abstract void EnergyLoss();

        void Start()
        {
            energy = 0;
        }
        void Update()
        {
            EnergyLoss();
        }

        public virtual void OnHit(int addedEnergy)
        {
            energy += addedEnergy;
        }
    }
}