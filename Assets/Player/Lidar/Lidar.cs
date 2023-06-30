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
        [SerializeField] int _numberOfBeamsScan;
        [Range(1, 45)]
        [SerializeField] int _numberOfBeamsFocused;
        [SerializeField] float _maxBeamDistance, _lineWidth;
        [SerializeField] GameObject _laserPrefab;

        [Header("Lidar Parameters")]
        [SerializeField] int _maxEnergy;
        [SerializeField] int _energyRefresh;
        [SerializeField] int _currentEnergy = 0;
        [Range(0f, 1f)]
        [SerializeField] float _percentageVariance;
        [SerializeField] int _lidarHorizontalSteps, _lidarVerticalSteps;
        [SerializeField] float _horizontalScanRange, _verticleScanRange;
        [SerializeField] float _horizontalScanTime, _verticleScanTime;
        [SerializeField] float _focusedScanRadius;
        [SerializeField] SetSlider energySlider;

        [Header("Particle Parameters")]
        [SerializeField] VisualEffect _VFX;

        List<LineRenderer> _beams = new List<LineRenderer>();
        Coroutine _lidarScan;
        LidarMode _currentMode;
        [SerializeField] EnergyLevels _energyLevel;
        bool _canRegen;

        enum LidarMode
        {
            Wide,
            Focused,
        }

        enum EnergyLevels
        {
            Low = 5,
            Medium = 10,
            High = 20,
        }

        private void Awake()
        {
            _currentMode = LidarMode.Wide;
            _energyLevel = EnergyLevels.Low;
            energySlider.SetSliderMax(_maxEnergy);
        }

        private void Update()
        {
            if (_lidarScan == null && _currentEnergy < _maxEnergy)
            {
                _currentEnergy += _energyRefresh;
                energySlider.SetSliderValue(_currentEnergy);
            }
        }

        public void StartScan(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                switch(_currentMode) {
                    case LidarMode.Wide:
                        _lidarScan = StartCoroutine(WideScan());
                        break;
                    case LidarMode.Focused:
                        _lidarScan = StartCoroutine(FocusedScan());
                        break;
                    default:
                        break;
                }
            }
            else if (context.canceled)
            {
                CancelScan();
            }
        }

        public void SwapModes(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _currentMode = EnumHelper.Next(_currentMode);
            }
        }

        public void IncrementEnergy(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _energyLevel = EnumHelper.Next(_energyLevel);
            }
        }

        public void CancelScan()
        {
            if (_lidarScan != null)
            {
                StopCoroutine(_lidarScan);
                LidarVFX.DestroyBeams(_beams);
                _lidarScan = null;
            }
        }

        IEnumerator WideScan()
        {
            float horizontalStepSize = _horizontalScanRange / _lidarHorizontalSteps;
            float verticalStepSize = _verticleScanRange / _lidarVerticalSteps;

            float horizontalStartingPoint = -_horizontalScanRange / 2;
            float verticalStartingSize = -_verticleScanRange / 2;

            float horizontalTimeStep = _horizontalScanTime / _lidarHorizontalSteps;
            float verticalTimeStep = _verticleScanTime / _lidarVerticalSteps;

            LidarVFX.CreatedBeams(_beams, _numberOfBeamsScan, _laserPrefab, this.transform, _lineWidth);

            for (int i = 0; i < _lidarVerticalSteps; i++)
            {
                for (int j = 0; j < _lidarHorizontalSteps; j += _numberOfBeamsScan)
                {
                    Vector3[] directions = new Vector3[_numberOfBeamsScan + 1];
                    for (int k = 0; k < _numberOfBeamsScan; k++)
                    {
                        int beamNum = j + k;
                        int isEven = beamNum % 2;

                        float xVarience = Random.Range(-_percentageVariance, _percentageVariance);
                        float yVarience = Random.Range(-_percentageVariance, _percentageVariance);

                        float xRotationAngle = verticalStartingSize + verticalStepSize * (i + yVarience);
                        float yRotationAngle = horizontalStartingPoint + horizontalStepSize * ((_lidarHorizontalSteps * isEven) + ((beamNum + xVarience) * Mathf.Pow(-1, isEven)));

                        directions[k] = Quaternion.AngleAxis(xRotationAngle, transform.right) * (Quaternion.AngleAxis(yRotationAngle, transform.up) * transform.forward);
                    }
                    LidarBeamBurst(directions);
                    yield return new WaitForSeconds(horizontalTimeStep);
                }
                yield return new WaitForSeconds(verticalTimeStep);
            }

            CancelScan();
        }

        IEnumerator FocusedScan()
        {
            LidarVFX.CreatedBeams(_beams, _numberOfBeamsFocused, _laserPrefab, this.transform, _lineWidth);

            while (true)
            {
                //create n direction vectors around the transform forward
                Vector3[] directions = new Vector3[_numberOfBeamsFocused];
                for (int i = 0; i < _numberOfBeamsFocused; i++)
                {
                    float xVarience, yVarience;

                    do
                    {
                        xVarience = Random.Range(-_focusedScanRadius, _focusedScanRadius);
                        yVarience = Random.Range(-_focusedScanRadius, _focusedScanRadius);
                    } while (Mathf.Sqrt(Mathf.Pow(xVarience, 2) + Mathf.Pow(yVarience, 2)) > _focusedScanRadius);

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
            int beamEnergy = (int)_energyLevel;

            if (_currentEnergy - (beamEnergy * length) <= 0)
            {
                CancelScan();
                return;
            }

            _currentEnergy -= (beamEnergy * length);
            energySlider.SetSliderValue(_currentEnergy);

            int particleInfoAmount = 3;
            Texture2D particleInfo = new Texture2D(_numberOfBeamsScan, particleInfoAmount, TextureFormat.RGBAFloat, false);

            for (int i = 0; i < length; i++)
            {
                RaycastHit hit = LidarBeam(directions[i], _beams[i % _numberOfBeamsScan]);
                LidarVFX.UpdateBeam(_beams[i % _numberOfBeamsScan]);

                if (hit.transform != null)
                {
                    Vector3 offset = hit.normal * 0.0001f;
                    Vector3 spawnPos = hit.point + offset;
                    float particleEnergy = beamEnergy * Random.Range(0.5f, 2f);

                    Quaternion rotationQuaternion = Quaternion.LookRotation(hit.normal, Vector3.up);
                    Vector3 normal = rotationQuaternion.eulerAngles;

                    particleInfo.SetPixel(i, 0, LidarVFX.Vector3ToVector4(spawnPos));
                    particleInfo.SetPixel(i, 1, LidarVFX.Vector3ToVector4(normal));
                    particleInfo.SetPixel(i, 2, new Color(191f/255f, 12f/255f, 12f/255f, 1f) * particleEnergy);
                }
            }

            particleInfo.Apply();
            LidarVFX.DrawParticles(particleInfo, _VFX, _numberOfBeamsScan, beamEnergy, beamEnergy + 0.5f);
        }

        RaycastHit LidarBeam(Vector3 direction, LineRenderer lineRenderer)
        {
            RaycastHit hit;

            Ray ray = new Ray(transform.position, direction);

            if (Physics.Raycast(ray, out hit, _maxBeamDistance))
            {
                ObjectMaterial[] materials = hit.transform.gameObject.GetComponents<ObjectMaterial>();

                if (materials.Length > 0)
                {
                    Debug.Log("material hit");
                    foreach (ObjectMaterial material in materials)
                    {
                        material.OnHit((int)_energyLevel);
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
                lineRenderer.SetPosition(1, transform.InverseTransformPoint(transform.position + direction * _maxBeamDistance));
            }

            return hit;
        }
    }
}