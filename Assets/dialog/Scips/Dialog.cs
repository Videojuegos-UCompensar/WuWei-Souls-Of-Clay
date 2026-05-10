using System.Collections;
using UnityEngine;
using TMPro;
public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] private GameObject dialogmark;
    [SerializeField]private GameObject Dialogpanel;
    [SerializeField] private TextMeshProUGUI Dialogtext;
    [SerializeField, TextArea(2, 5)] private string[] dialoglines;
   private bool isplayerinrange;
   private bool dialoguestart;
   private int lineindex;
private float typingspeed = 0.05f;
       void Update()
    {
        if(isplayerinrange && Input.GetKeyDown(KeyCode.L))
        {
            if (!dialoguestart)
            {
                StartDialog();
            }
            else if (Dialogtext.text == dialoglines[lineindex])
            {
                
                NextLine();
                }
            else
            {
                StopAllCoroutines();
                Dialogtext.text = dialoglines[lineindex];
            }
            
                
            }  
        }
            private void StartDialog()
            {
                dialoguestart = true;
                Dialogpanel.SetActive(true);
                dialogmark.SetActive(false);
                lineindex = 0;
                Time.timeScale = 0f;
                StartCoroutine(Showline());
            }
private void NextLine()
    {
        lineindex++;
        if (lineindex < dialoglines.Length - 1)
        {
            
    
            StartCoroutine(Showline());
        }
        else
        {
            Dialogpanel.SetActive(false);
            dialoguestart = false;
            Time.timeScale = 1f;
        }
    }
private IEnumerator Showline()
    {
        Dialogtext.text = string.Empty;
        foreach (char ch in dialoglines[lineindex])
        {
            Dialogtext.text += ch;
            yield return new WaitForSecondsRealtime(typingspeed);
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
      
        if (collision.gameObject.CompareTag("Player"))
            isplayerinrange = true;
            dialogmark.SetActive(true);
            Debug.Log("Player in range");
        
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
            if (collision.gameObject.CompareTag("Player"))
            isplayerinrange = false;
            dialogmark.SetActive(false);
            Debug.Log("Player out of range");
    }
   
}
