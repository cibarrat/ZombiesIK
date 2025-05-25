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

    public void SaveData ()
    {

        days = int.Parse(daysInput.text);
        killed = int.Parse(killedInput.text);

        PlayerPrefs.SetString("NameKey", nameInput.text);
        PlayerPrefs.SetInt("DaysKey", days);
        PlayerPrefs.SetInt("KilledKey", killed);

        nameText.text = PlayerPrefs.GetString("NameKey");
        daysText.text = PlayerPrefs.GetInt("DaysKey").ToString();
        killedText.text = PlayerPrefs.GetInt("KilledKey").ToString();
    }

    public void EraseData()
    {
        PlayerPrefs.DeleteAll();

        nameInput.text = ""; 
        daysInput.text = ""; 
        killedInput.text = ""; 

        // PlayerPrefs.DeleteKey("AgeKey");    Codigo para borrar solamente uno de los inputs: 
        nameText.text = "Name";
        daysText.text = "Days";
        killedText.text = "Killed";


    }

    /*
    public void LoadData()
    {

    }*/



}
