using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class SaveSystem : MonoBehaviour
{

    [SerializeField] TMP_InputField nameInput;
    [SerializeField] TMP_InputField daysInput;
    [SerializeField] TMP_InputField killedInput;

    private int days;
    private int killed;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text daysText;
    [SerializeField] private TMP_Text killedText;

    public void Awake()
    {
        nameText.text = PlayerPrefs.GetString("NameKey");
        daysText.text = PlayerPrefs.GetInt("deaths").ToString();
        killedText.text = PlayerPrefs.GetInt("killed").ToString();
    }


    public void SaveData ()
    {

        PlayerPrefs.SetString("NameKey", nameInput.text);
        nameText.text = PlayerPrefs.GetString("NameKey");
    }

    public void EraseData()
    {
        PlayerPrefs.DeleteAll();

        nameInput.text = ""; 
        daysInput.text = ""; 
        killedInput.text = ""; 

        // PlayerPrefs.DeleteKey("AgeKey");    Codigo para borrar solamente uno de los inputs: 
        nameText.text = "";
        daysText.text = "0";
        killedText.text = "0";


    }

    /*
    public void LoadData()
    {

    }*/



}
