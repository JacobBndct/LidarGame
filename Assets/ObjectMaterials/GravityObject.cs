using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LidarMaterial
{
    public class GravityObject : ObjectMaterial
    {
        Rigidbody body;

        // Start is called before the first frame update
        void Start()
        {
            body = GetComponent<Rigidbody>();
        }

        public override void OnHit(int addedEnergy)
        {
            base.OnHit(addedEnergy);
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.None;
        }

        protected override void EnergyLoss()
        {
            if (energy <= 0)
            {
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeAll;
            }
            else
            {
                energy--;
            }
        }
    }
}
