using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{

    GameObject AnyKeyTextObject;
    GameObject HighScoreTextObject;
    private float HighScore
    {
        get { return PlayerPrefs.GetInt("HighScore"); }
    }


    public void Awake()
    {
        AnyKeyTextObject = GameObject.Find("Play Button Text");
        if (AnyKeyTextObject == null)
        {
            Debug.LogError("Play Button Text not found");
        }

        HighScoreTextObject = GameObject.Find("High Score Text");
        if (HighScoreTextObject == null)
        {
            Debug.LogError("High Score Text not found.");
        }

        HighScoreTextObject.GetComponent<TextMeshProUGUI>().text = "High Score: " + HighScore.ToString("0000");

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
