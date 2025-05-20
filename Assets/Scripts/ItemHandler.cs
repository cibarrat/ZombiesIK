using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHandler : MonoBehaviour
{
    [field:SerializeField] public AudioClip PickupSound { get; set; }
    public int Quantity = 1;
}
