using UnityEngine;
using System.Collections.Generic;

public class DictionaryTest : MonoBehaviour
{
    // 지금까지 배웠던 배열이나 List는 인덱스를 이용해 요소에 접근했다.
    // Dictionary는 요소를 저장할 때 "키"와 "값"을 쌍으로 저장한다.
    // HashTable(=Dictionary)는 키와 값이 쌍으로 이루어진 데이터를 관리(CRUDS)할 때 사용하는 자료구조

    private void Start()
    {
        List<int> args = new List<int>() { 22, 21, 20, 35 };
        Dictionary<string, int> students = new Dictionary<string, int>()
            { { "오나현", 22 }, { "박철규", 21 }, { "최재원", 20 } };

        Debug.Log(students["오나현"]);
        students["박철규"] = 23;

        foreach (KeyValuePair<string, int> item in students)
        {
            Debug.Log($"{item.Key}의 나이 : {item.Value}");
        }
    }
}