using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{

    float LifeTime;
    GameObject ParticleObject;

    public void StartParticle(string ParticleName, Vector2 ParticlePosition, float LifeTime)
    {
        this.LifeTime = LifeTime;
        ParticleObject = Instantiate(Resources.Load<GameObject>(ParticleName), ParticlePosition, Quaternion.identity);
        if(ParticleObject == null)
        {
            Debug.LogError("Particle prefab not found in resources: " + ParticleName);
        }

        ParticleObject.GetComponent<ParticleSystem>().Play();
        StartCoroutine("ParticleCountdown");

    }

    IEnumerator ParticleCountdown()
    {
        yield return new WaitForSeconds(LifeTime);
        Destroy(ParticleObject);
        Destroy(gameObject);
    }

}
