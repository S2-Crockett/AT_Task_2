using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class TextManager : MonoBehaviour
{
    public GameObject UI;
    public Text text;
    public Grid grid;
    
    private bool set = false;

    public string[] newText;
    public string clone = "(Clone)";
    
    
    
    // Update is called once per frame
    void Update()
    {
        if (grid._moveObstacles == MoveObstacles.PathCreated)
        {
            if (!set)
            {
                newText = new string[grid.ObstaclesList[0].changeableGameObjects.Count];
                for (int i = 0; i < newText.Length; i++)
                {
                    newText[i] = grid.ObstaclesList[0].changeableGameObjects[i].name;
                }
                set = true;
            }
            text.text = "Change " + grid.ObstaclesList[0].hitColliders.name + " With:";

            
            for(int i = 0; i < grid.ObstaclesList[0].changeableGameObjects.Count; i++)
            {
                newText[i] = newText[i].Replace(clone, "");
                text.text += "\n" + newText[i];
            }
            UI.SetActive(true);
        }
    }
}
