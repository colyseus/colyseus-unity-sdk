using UnityEngine;
using System.Collections;


namespace TMPro.Examples
{
    public class TextMeshProFloatingText : MonoBehaviour
    {
        public Font TheFont;

        private GameObject m_floatingText;
        private TextMeshPro m_textMeshPro;
        private TextMesh m_textMesh;

        private Transform m_transform;
        private Transform m_floatingText_Transform;
        private Transform m_cameraTransform;

        private Vector3 lastPOS = Vector3.zero;
        private Quaternion lastRotation = Quaternion.identity;

        public int SpawnType;
        public bool IsTextObjectScaleStatic;

        //private int m_frame = 0;

        private static WaitForEndOfFrame k_WaitForEndOfFrame = new();

        private static WaitForSeconds[] k_WaitForSecondsRandom = new WaitForSeconds[]
        {
            new(0.05f), new(0.1f), new(0.15f), new(0.2f), new(0.25f),
            new(0.3f), new(0.35f), new(0.4f), new(0.45f), new(0.5f),
            new(0.55f), new(0.6f), new(0.65f), new(0.7f), new(0.75f),
            new(0.8f), new(0.85f), new(0.9f), new(0.95f), new(1.0f)
        };

        private void Awake()
        {
            m_transform = transform;
            m_floatingText = new GameObject(name + " floating text");

            // Reference to Transform is lost when TMP component is added since it replaces it by a RectTransform.
            //m_floatingText_Transform = m_floatingText.transform;
            //m_floatingText_Transform.position = m_transform.position + new Vector3(0, 15f, 0);

            m_cameraTransform = Camera.main.transform;
        }

        private void Start()
        {
            if (SpawnType == 0)
            {
                // TextMesh Pro Implementation
                m_textMeshPro = m_floatingText.AddComponent<TextMeshPro>();
                m_textMeshPro.rectTransform.sizeDelta = new Vector2(3, 3);

                m_floatingText_Transform = m_floatingText.transform;
                m_floatingText_Transform.position = m_transform.position + new Vector3(0, 15f, 0);

                //m_textMeshPro.fontAsset = Resources.Load("Fonts & Materials/JOKERMAN SDF", typeof(TextMeshProFont)) as TextMeshProFont; // User should only provide a string to the resource.
                //m_textMeshPro.fontSharedMaterial = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(Material)) as Material;

                m_textMeshPro.alignment = TextAlignmentOptions.Center;
                m_textMeshPro.color = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255),
                    (byte)Random.Range(0, 255), 255);
                m_textMeshPro.fontSize = 24;
                //m_textMeshPro.enableExtraPadding = true;
                //m_textMeshPro.enableShadows = false;
                m_textMeshPro.enableKerning = false;
                m_textMeshPro.text = string.Empty;
                m_textMeshPro.isTextObjectScaleStatic = IsTextObjectScaleStatic;

                StartCoroutine(DisplayTextMeshProFloatingText());
            }
            else if (SpawnType == 1)
            {
                //Debug.Log("Spawning TextMesh Objects.");

                m_floatingText_Transform = m_floatingText.transform;
                m_floatingText_Transform.position = m_transform.position + new Vector3(0, 15f, 0);

                m_textMesh = m_floatingText.AddComponent<TextMesh>();
                m_textMesh.font = Resources.Load<Font>("Fonts/ARIAL");
                m_textMesh.GetComponent<Renderer>().sharedMaterial = m_textMesh.font.material;
                m_textMesh.color = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255),
                    (byte)Random.Range(0, 255), 255);
                m_textMesh.anchor = TextAnchor.LowerCenter;
                m_textMesh.fontSize = 24;

                StartCoroutine(DisplayTextMeshFloatingText());
            }
            else if (SpawnType == 2)
            {
            }
        }


        //void Update()
        //{
        //    if (SpawnType == 0)
        //    {
        //        m_textMeshPro.SetText("{0}", m_frame);
        //    }
        //    else
        //    {
        //        m_textMesh.text = m_frame.ToString();
        //    }
        //    m_frame = (m_frame + 1) % 1000;

        //}


        public IEnumerator DisplayTextMeshProFloatingText()
        {
            var CountDuration = 2.0f; // How long is the countdown alive.
            var starting_Count = Random.Range(5f, 20f); // At what number is the counter starting at.
            var current_Count = starting_Count;

            var start_pos = m_floatingText_Transform.position;
            Color32 start_color = m_textMeshPro.color;
            float alpha = 255;
            var int_counter = 0;


            var fadeDuration = 3 / starting_Count * CountDuration;

            while (current_Count > 0)
            {
                current_Count -= Time.deltaTime / CountDuration * starting_Count;

                if (current_Count <= 3)
                    //Debug.Log("Fading Counter ... " + current_Count.ToString("f2"));
                    alpha = Mathf.Clamp(alpha - Time.deltaTime / fadeDuration * 255, 0, 255);

                int_counter = (int)current_Count;
                m_textMeshPro.text = int_counter.ToString();
                //m_textMeshPro.SetText("{0}", (int)current_Count);

                m_textMeshPro.color = new Color32(start_color.r, start_color.g, start_color.b, (byte)alpha);

                // Move the floating text upward each update
                m_floatingText_Transform.position += new Vector3(0, starting_Count * Time.deltaTime, 0);

                // Align floating text perpendicular to Camera.
                if (!lastPOS.Compare(m_cameraTransform.position, 1000) ||
                    !lastRotation.Compare(m_cameraTransform.rotation, 1000))
                {
                    lastPOS = m_cameraTransform.position;
                    lastRotation = m_cameraTransform.rotation;
                    m_floatingText_Transform.rotation = lastRotation;
                    var dir = m_transform.position - lastPOS;
                    m_transform.forward = new Vector3(dir.x, 0, dir.z);
                }

                yield return k_WaitForEndOfFrame;
            }

            //Debug.Log("Done Counting down.");

            yield return k_WaitForSecondsRandom[Random.Range(0, 19)];

            m_floatingText_Transform.position = start_pos;

            StartCoroutine(DisplayTextMeshProFloatingText());
        }


        public IEnumerator DisplayTextMeshFloatingText()
        {
            var CountDuration = 2.0f; // How long is the countdown alive.
            var starting_Count = Random.Range(5f, 20f); // At what number is the counter starting at.
            var current_Count = starting_Count;

            var start_pos = m_floatingText_Transform.position;
            Color32 start_color = m_textMesh.color;
            float alpha = 255;
            var int_counter = 0;

            var fadeDuration = 3 / starting_Count * CountDuration;

            while (current_Count > 0)
            {
                current_Count -= Time.deltaTime / CountDuration * starting_Count;

                if (current_Count <= 3)
                    //Debug.Log("Fading Counter ... " + current_Count.ToString("f2"));
                    alpha = Mathf.Clamp(alpha - Time.deltaTime / fadeDuration * 255, 0, 255);

                int_counter = (int)current_Count;
                m_textMesh.text = int_counter.ToString();
                //Debug.Log("Current Count:" + current_Count.ToString("f2"));

                m_textMesh.color = new Color32(start_color.r, start_color.g, start_color.b, (byte)alpha);

                // Move the floating text upward each update
                m_floatingText_Transform.position += new Vector3(0, starting_Count * Time.deltaTime, 0);

                // Align floating text perpendicular to Camera.
                if (!lastPOS.Compare(m_cameraTransform.position, 1000) ||
                    !lastRotation.Compare(m_cameraTransform.rotation, 1000))
                {
                    lastPOS = m_cameraTransform.position;
                    lastRotation = m_cameraTransform.rotation;
                    m_floatingText_Transform.rotation = lastRotation;
                    var dir = m_transform.position - lastPOS;
                    m_transform.forward = new Vector3(dir.x, 0, dir.z);
                }

                yield return k_WaitForEndOfFrame;
            }

            //Debug.Log("Done Counting down.");

            yield return k_WaitForSecondsRandom[Random.Range(0, 20)];

            m_floatingText_Transform.position = start_pos;

            StartCoroutine(DisplayTextMeshFloatingText());
        }
    }
}