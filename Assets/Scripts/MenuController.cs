using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{

    GameObject AnyKeyTextObject;
    GameObject HighScoreTextObject;
    bool MenuReactive = true;
    Coroutine BlinkTextRoutine;
    float BlinkTextDelay = 0.75f;

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
        ResetBlink();
        GameAssets.Sound.MenuMusic.Play();
    }

    IEnumerator BlinkPlayText()
    {
        while (true)
        {
            yield return new WaitForSeconds(BlinkTextDelay);
            AnyKeyTextObject.SetActive(false);
            yield return new WaitForSeconds(BlinkTextDelay);
            AnyKeyTextObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!MenuReactive) return;

        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            MenuReactive = false;
            GameAssets.Sound.MenuMusic.Stop();
            StartCoroutine("PlayGameSequence");

            return;
        }
    }


    void ResetBlink()
    {
        if (BlinkTextRoutine != null) StopCoroutine(BlinkTextRoutine);
        BlinkTextRoutine = StartCoroutine("BlinkPlayText");
    }

    IEnumerator PlayGameSequence()
    {
        BlinkTextDelay = 0.1f;
        ResetBlink();
        GameAssets.Sound.PlaySound.Play();
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(1);
    }

}
