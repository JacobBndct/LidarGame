using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace Tools.Lidar
{
    public class LidarVFX : MonoBehaviour
    {
        public static List<LineRenderer> CreatedBeams(List<LineRenderer> beams, int newBeams, GameObject laserPrefab, Transform transform, float lineWidth)
        {
            for (int i = 0; i < newBeams; i++)
            {
                beams.Add(DrawLine(laserPrefab, transform, lineWidth));
            }

            return beams;
        }

        public static List<LineRenderer> DestroyBeams(List<LineRenderer> beams)
        {
            foreach (LineRenderer beam in beams)
            {
                Destroy(beam.gameObject);
            }
            beams.Clear();

            return beams;
        }

        public static LineRenderer DrawLine(GameObject laserPrefab, Transform transform, float lineWidth)
        {
            GameObject line = Instantiate(laserPrefab, transform);
            LineRenderer lineRenderer = line.GetComponents<LineRenderer>().First();

            lineRenderer.startWidth = lineWidth;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);

            return lineRenderer;
        }

        public static void UpdateBeam(LineRenderer line)
        {
            Color colour = line.startColor;
            float alpha = Random.Range(0.0f, 1.0f);
            line.startColor = SetColourAlpha(colour, alpha);
        }

        public static VisualEffect DrawParticles(Texture2D particleInfo, VisualEffect VFX, int numberOfBeams, float maxLifetime, float minLifetime)
        {
            VFX.SetInt("Number of Particles", numberOfBeams);
            VFX.SetTexture("Particle Info", particleInfo);
            VFX.SetFloat("Min Lifetime", maxLifetime);
            VFX.SetFloat("Max Lifetime", minLifetime);
            VFX.SendEvent("OnPlay");

            return VFX;
        }

        //put into a seperate file
        public static Vector4 Vector3ToVector4(Vector3 vector3)
        {
            return new Vector4(vector3.x, vector3.y, vector3.z, 0f);
        }

        static Color SetColourAlpha(Color colour, float alpha)
        {
            return new Color(colour.r, colour.g, colour.b, alpha);
        }
    }
}
