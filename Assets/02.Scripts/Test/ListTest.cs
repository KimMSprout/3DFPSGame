using System.Collections.Generic;
using UnityEngine;

public class ListTest : MonoBehaviour
{
    // 자료 구조 (Data Structure)

    private void Start()
    {
        int[] ages = new int[] { 22, 21, 20, 35 };

        // 1. 배열의 선언할 때 정한 크기는 변경 불가능
        ages[0] = 10;
        ages[1] = 40;
        ages[2] = 20;
        ages[3] = 30;
        // ages[4] = 50; // IndexOutOfRangeException 발생

        List<int> ages2 = new List<int>() { 10, 20, 30, 40 };

        ages2[0] = 15;
        ages2[1] = 25;
        ages2[2] = 35;
        ages2[3] = 45;
        // ages2[4] = 55; // ArgumentOutOfRangeException
        ages2.Add(55);
        ages2.Insert(1, 20);

        for (int i = 0; i < ages2.Count; i++)
        {
            Debug.Log(ages2[i]); // 15 20 25 35 45 55
        }

        ages2.Remove(45); // 15 20 25 35 55
        ages2.RemoveAt(3); // 15 20 25 55

        foreach (int age in ages2)
        {
            Debug.Log(age);
        }
    }
}