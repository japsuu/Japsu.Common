using System;
using UnityEngine;

namespace Japsu.Common.InteractionSystem
{
    public class InteractionManager : MonoBehaviour
    {
        public static event Action<IInteractable> OnInteractablePointedAt; 

        [SerializeField]
        private KeyCode interactKey = KeyCode.F;

        [SerializeField]
        private LayerMask interactLayer;

        [SerializeField]
        private float interactDistance;

        public bool InteractionEnabled = true;
        private Camera mainCam;

        private void Awake()
        {
            mainCam = Camera.main;
        }

        private void Update()
        {
            if(!InteractionEnabled) return;
        
            if(mainCam == null) return;
        
            Ray camForward = new Ray(mainCam.transform.position, mainCam.transform.forward);

            // Raycast if we hit an object
            if (!Physics.Raycast(camForward, out RaycastHit hit, interactDistance, interactLayer)) return;
        
            // Check if the object is interactable
            IInteractable interactable = hit.collider.gameObject.GetComponent<IInteractable>();
        
            if (interactable == null) return;
                
            // Display the UI text
            OnInteractablePointedAt?.Invoke(interactable);

            if (interactable.CanBeInteractedWith() && Input.GetKeyDown(interactKey))
            {
                interactable.Interact();
            }
        }
    }
}
