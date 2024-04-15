using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

namespace Assets.Scripts
{
    public class ScreenWipe : MonoBehaviour
    {
        public static ScreenWipe Instance;

        [SerializeField]
        [Range(0.1f, 1f)]
        private float wipeSpeed = 1f;

        private Image image;

        private enum WipeMode { NotBlocked, WipingToNotBlocked, Blocked, WipingToBlocked }

        private WipeMode wipeMode = WipeMode.NotBlocked;

        private float wipeProgress;

        public bool isDone { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            image = GetComponentInChildren<Image>();
        }

        public void ToggleWipe(bool blockScreen)
        {
            isDone = false;
            if (blockScreen)
            {
                wipeMode = WipeMode.WipingToBlocked;
            }
            else
            {
                wipeMode = WipeMode.WipingToNotBlocked;
            }
        }


        void Update()
        {
            switch (wipeMode)
            {
                case WipeMode.WipingToBlocked:
                    WipeToBlocked();
                    break;
                case WipeMode.WipingToNotBlocked:
                    WipeToNotBlocked();
                    break;
            }
        }

        public float blockRotation = 180f;
        public float clearRotation = 0f;

        private void WipeToBlocked()
        {
            image.transform.eulerAngles = new Vector3(0f, 0f, clearRotation);
            wipeProgress += Time.deltaTime * (1f / wipeSpeed);
            image.fillAmount = wipeProgress;
            if (wipeProgress >= 1f)
            {
                isDone = true;
                wipeMode = WipeMode.Blocked;
            }
        }

        private void WipeToNotBlocked()
        {
            image.transform.eulerAngles = new Vector3(0f, 0f, blockRotation);
            wipeProgress -= Time.deltaTime * (1f / wipeSpeed);
            image.fillAmount = wipeProgress;
            if (wipeProgress <= 0f)
            {
                isDone = true;
                wipeMode = WipeMode.NotBlocked;
            }
        }

        [ContextMenu("Block")]
        private void Block() { ToggleWipe(true); }
        [ContextMenu("Clear")]
        private void Clear() { ToggleWipe(false); }


    }
}
