using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterInteraction : MonoBehaviour
{
    [SerializeField] private ParticleSystem _interactionParticleSystem;
    private ParticleSystem.EmissionModule emissionModule;

    private void Start()
    {
        if (_interactionParticleSystem.gameObject.activeSelf)
        {
            _interactionParticleSystem.gameObject.SetActive(false);
        }

        emissionModule = _interactionParticleSystem.emission;
        emissionModule.enabled = false;
    }

    private void OnTriggerEnter(Collider water)
    {
        if (!_interactionParticleSystem.gameObject.activeSelf)
        {
            _interactionParticleSystem.gameObject.SetActive(true);
        }

        emissionModule.enabled = true; //Enable the ripple emission when player is in the water
    }

    private void OnTriggerStay(Collider water)
    {
        float waterHeight = water.transform.position.y;
        _interactionParticleSystem.transform.position = new Vector3(gameObject.transform.position.x, waterHeight, gameObject.transform.position.z);
    }

    private void OnTriggerExit(Collider water)
    {
        emissionModule.enabled = false; //Disable the ripple emission when player is in the water
    }
}
