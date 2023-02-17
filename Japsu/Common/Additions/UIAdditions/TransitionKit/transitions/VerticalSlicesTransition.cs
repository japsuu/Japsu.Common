using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Japsu.Common.Additions.UIAdditions.TransitionKit.transitions
{
    public class VerticalSlicesTransition : TransitionKitDelegate
    {
        public float duration = 0.5f;
        public int nextScene = -1;
        public int divisions = 5;

        private QuadSlice[] _quadSlices;


        private class QuadSlice
        {
            private int[] _vertIndices = new int[4];
            private Vector3[] _initialPositions = new Vector3[4];


            public QuadSlice(int firstVertIndex, Vector3[] verts)
            {
                for (int i = 0; i < 4; i++)
                {
                    _vertIndices[i] = firstVertIndex + i;
                    _initialPositions[i] = verts[_vertIndices[i]];
                }
            }


            public void shiftVerts(Vector3 offset, Vector3[] verts)
            {
                for (int i = 0; i < 4; i++)
                    verts[_vertIndices[i]] = _initialPositions[i] + offset;
            }
        }


        #region TransitionKitDelegate implementation

        public Shader shaderForTransition()
        {
            return null;
        }


        public Mesh meshForDisplay()
        {
            // we need at least 2 divisions
            if (divisions < 2)
                divisions = 2;

            _quadSlices = new QuadSlice[divisions];
            Mesh mesh = new Mesh();

            // figure out how many verts and triangles we need
            int vertsPerRow = divisions * 2;
            int numTriangles = divisions * 6; // 2 tris per division slice with 3 verts each
            int numVertices = vertsPerRow * 2; // top and bottom rows

            Vector3[] verts = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] tris = new int[numTriangles];

            // so, our verts need to go from -halfWidth to halfWidth and -halfHeight to halfHeight
            float halfHeight = 5f; // 5 is the camera.orthoSize which is half the screen height
            float halfWidth = halfHeight * ((float)Screen.width / (float)Screen.height);
            float width = halfWidth * 2f;
            float divisionWidth = 1.0f / divisions * width;
            float divisionWidthFraction =
                divisionWidth / width; // width of a slice normalized from 0 to 1 for uv generation

            // create our verts, tris and uvs
            int index = 0;
            int triIndex = 0;
            for (int i = 0; i < divisions; i++)
            {
                int rootVertIndex = i * 4; // first vert index in each loop iteration
                float xMin = i * divisionWidth - halfWidth;
                float xMax = xMin + divisionWidth;
                float uvMin = i * divisionWidthFraction;
                float uvMax = uvMin + divisionWidthFraction;

                verts[index++] = new Vector3(xMin, -halfHeight, 0);
                verts[index++] = new Vector3(xMin, halfHeight, 0);
                verts[index++] = new Vector3(xMax, -halfHeight, 0);
                verts[index++] = new Vector3(xMax, halfHeight, 0);

                tris[triIndex++] = 0 + rootVertIndex;
                tris[triIndex++] = 1 + rootVertIndex;
                tris[triIndex++] = 2 + rootVertIndex;
                tris[triIndex++] = 3 + rootVertIndex;
                tris[triIndex++] = 2 + rootVertIndex;
                tris[triIndex++] = 1 + rootVertIndex;

                uvs[rootVertIndex + 0] = new Vector2(uvMin, 0);
                uvs[rootVertIndex + 1] = new Vector2(uvMin, 1);
                uvs[rootVertIndex + 2] = new Vector2(uvMax, 0);
                uvs[rootVertIndex + 3] = new Vector2(uvMax, 1);

                _quadSlices[i] = new QuadSlice(rootVertIndex, verts);
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;

            return mesh;
        }


        public Texture2D textureForDisplay()
        {
            return null;
        }


        public IEnumerator onScreenObscured(TransitionKit transitionKit)
        {
            transitionKit.transitionKitCamera.clearFlags = CameraClearFlags.Nothing;

            // we dont transition back to the new scene unless it is loaded
            if (nextScene >= 0)
            {
                SceneManager.LoadSceneAsync(nextScene);
                yield return transitionKit.StartCoroutine(transitionKit.waitForLevelToLoad(nextScene));
            }

            float transitionDistance =
                transitionKit.GetComponent<Camera>().orthographicSize *
                2f; // 2x our camera.orthoSize so we move the slices off screen
            float elapsed = 0f;
            Mesh mesh = transitionKit.GetComponent<MeshFilter>().mesh;
            Vector3[] verts = mesh.vertices;

            while (elapsed < duration)
            {
                elapsed += transitionKit.deltaTime;
                float step = Mathf.Pow(elapsed / duration, 2f);
                float offset = Mathf.Lerp(0, transitionDistance, step);

                // transition our QuadSlices
                for (int i = 0; i < _quadSlices.Length; i++)
                {
                    // odd ones move up, even down
                    float sign = i % 2 == 0 ? 1f : -1f;
                    _quadSlices[i].shiftVerts(new Vector3(0, offset * sign), verts);
                }

                // reassign our verts
                mesh.vertices = verts;

                yield return null;
            }
        }

        #endregion
    }
}