using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.VFX;
using LidarMaterial;
using static UnityEditor.PlayerSettings;

namespace Tools.Lidar
{
    public class Lidar : MonoBehaviour
    {
        [Header("Line Parameters")]
        [Range(1, 90)]
        [SerializeField] int numberOfBeams;
        [SerializeField] float maxBeamDistance, lineWidth;
        [SerializeField] Material lineMaterial;
        [SerializeField] GameObject laserPrefab;

        [Header("Lidar Parameters")]
        [SerializeField] int beamEnergy;
        [SerializeField] int maxEnergy;
        [SerializeField] int energyRefresh;
        [SerializeField] int currentEnergy = 0;
        [Range(0f, 1f)]
        [SerializeField] float percentageVariance;
        [SerializeField] int lidarHorizontalSteps, lidarVerticalSteps;
        [SerializeField] float horizontalScanRange, verticleScanRange;
        [SerializeField] float horizontalScanTime, verticleScanTime;

        [Header("Particle Parameters")]
        [SerializeField] VisualEffect VFX;

        List<LineRenderer> beams = new List<LineRenderer>();
        Coroutine lidarScan;

        private void Update()
        {
            if (lidarScan == null && currentEnergy < maxEnergy)
            {
                currentEnergy += energyRefresh;
            }
        }

        public void StartLidarScan(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                lidarScan = StartCoroutine(LidarScan());
            }
            else if (context.canceled)
            {
                CancelLidarScan();
            }
        }

        public void StartFocusedLidarScan(InputAction.CallbackContext context)
        {
            if (context.performed && lidarScan == null)
            {
                lidarScan = StartCoroutine(FocusedLidarScan());
            }
            else if (context.canceled)
            {
                CancelLidarScan();
            }
        }

        public void CancelLidarScan()
        {
            if (lidarScan != null)
            {
                StopCoroutine(lidarScan);
                LidarVFX.DestroyBeams(beams);
                lidarScan = null;
            }
        }

        IEnumerator LidarScan()
        {
            float horizontalStepSize = horizontalScanRange / lidarHorizontalSteps;
            float verticalStepSize = verticleScanRange / lidarVerticalSteps;

            float horizontalStartingPoint = -horizontalScanRange / 2;
            float verticalStartingSize = -verticleScanRange / 2;

            float horizontalTimeStep = horizontalScanTime / lidarHorizontalSteps;
            float verticalTimeStep = verticleScanTime / lidarVerticalSteps;

            LidarVFX.CreatedBeams(beams, numberOfBeams, laserPrefab, this.transform, lineWidth);

            for (int i = 0; i < lidarVerticalSteps; i++)
            {
                for (int j = 0; j < lidarHorizontalSteps; j += numberOfBeams)
                {
                    Vector3[] directions = new Vector3[numberOfBeams + 1];
                    for (int k = 0; k < numberOfBeams; k++)
                    {
                        int beamNum = j + k;
                        int isEven = beamNum % 2;

                        float xVarience = Random.Range(-percentageVariance, percentageVariance);
                        float yVarience = Random.Range(-percentageVariance, percentageVariance);

                        float xRotationAngle = verticalStartingSize + verticalStepSize * (i + yVarience);
                        float yRotationAngle = horizontalStartingPoint + horizontalStepSize * ((lidarHorizontalSteps * isEven) + ((beamNum + xVarience) * Mathf.Pow(-1, isEven)));

                        Vector3 direction = Quaternion.AngleAxis(xRotationAngle, transform.right) * (Quaternion.AngleAxis(yRotationAngle, transform.up) * transform.forward);
                        directions[k] = direction;
                    }
                    LidarBeamBurst(directions);
                    yield return new WaitForSeconds(horizontalTimeStep);
                }
                yield return new WaitForSeconds(verticalTimeStep);
            }

            LidarVFX.DestroyBeams(beams);
            CancelLidarScan();
        }

        IEnumerator FocusedLidarScan()
        {
            int newBeams = numberOfBeams / 10;
            LidarVFX.CreatedBeams(beams, newBeams, laserPrefab, this.transform, lineWidth);

            while (true)
            {
                //create n direction vectors around the transform forward
                Vector3[] directions = new Vector3[newBeams];
                for (int i = 0; i < newBeams; i++)
                {
                    float xVarience, yVarience;

                    do
                    {
                        xVarience = Random.Range(-percentageVariance * 10, percentageVariance * 10);
                        yVarience = Random.Range(-percentageVariance * 10, percentageVariance * 10);
                    } while (Mathf.Sqrt(Mathf.Pow(xVarience, 2) + Mathf.Pow(yVarience, 2)) > percentageVariance * 10);

                    directions[i] = Quaternion.AngleAxis(xVarience, transform.right) * (Quaternion.AngleAxis(yVarience, transform.up) * transform.forward);
                }

                //call LidarBeamBurst for those n directions
                LidarBeamBurst(directions);
                yield return null;
            }
        }

        void LidarBeamBurst(Vector3[] directions)
        {
            int length = directions.Length - 1;

            int particleInfoAmount = 2;
            Texture2D particleInfo = new Texture2D(numberOfBeams, particleInfoAmount, TextureFormat.RGBAFloat, false);

            for (int i = 0; i < length; i++)
            {
                RaycastHit hit = LidarBeam(directions[i], beams[i % numberOfBeams]);
                LidarVFX.UpdateBeam(beams[i % numberOfBeams]);

                Vector3 offset = hit.normal * 0.0001f;
                Vector3 spawnPos = hit.point + offset;

                Quaternion rotationQuaternion = Quaternion.LookRotation(hit.normal, Vector3.up);
                Vector3 normal = rotationQuaternion.eulerAngles;

                //Debug.DrawRay(spawnPos, normal);
                particleInfo.SetPixel(i, 0, LidarVFX.Vector3ToVector4(spawnPos));
                particleInfo.SetPixel(i, 1, LidarVFX.Vector3ToVector4(normal));

                currentEnergy -= beamEnergy;
                if (currentEnergy < 0)
                {
                    CancelLidarScan();
                    break;
                }
            }

            particleInfo.Apply();
            LidarVFX.DrawParticles(particleInfo, VFX, numberOfBeams, beamEnergy, beamEnergy + 0.5f);
        }

        RaycastHit LidarBeam(Vector3 direction, LineRenderer lineRenderer)
        {
            RaycastHit hit;

            Ray ray = new Ray(transform.position, direction);

            if (Physics.Raycast(ray, out hit, maxBeamDistance))
            {
                ObjectMaterial[] materials = hit.transform.gameObject.GetComponents<ObjectMaterial>();
                if (materials.Length > 0)
                {
                    Debug.Log("material hit");
                    foreach (ObjectMaterial material in materials)
                    {
                        material.OnHit(beamEnergy);
                    }
                }
                else
                {
                    Debug.Log("default hit");
                }
                lineRenderer.SetPosition(1, transform.InverseTransformPoint(hit.point));
            }
            else
            {
                Debug.Log("Miss");
                lineRenderer.SetPosition(1, transform.InverseTransformPoint(direction * maxBeamDistance));
            }

            return hit;
        }
    }
}