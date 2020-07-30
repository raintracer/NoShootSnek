using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.WSA.Input;

public class MenuController : MonoBehaviour
{

    GameObject AnyKeyTextObject;

    public void Awake()
    {
        AnyKeyTextObject = GameObject.Find("Play Button Text");
        if (AnyKeyTextObject == null)
        {
            Debug.LogError("Play Button Text not found");
        }
    }

    public void Start()
    {
        StartCoroutine("BlinkPlayText");
    }

    IEnumerator BlinkPlayText()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            AnyKeyTextObject.SetActive(false);
            yield return new WaitForSeconds(1);
            AnyKeyTextObject.SetActive(true);
        }
    }

    void Update()
    {
        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(1);
        }
    }

    void PlayGame()
    {
        // SceneManager.LoadScene(1);
    }

}
