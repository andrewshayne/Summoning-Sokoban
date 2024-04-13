#if (UNITY_EDITOR) 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class Snap : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        // only snap while in edit mode, if not playing and not in prefab edit mode
        if(!Application.isPlaying && UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
        {
            SnapToGrid2();
        }
    }

    private void SnapToGrid()
    {
        Vector3 position = new Vector3(
            Mathf.RoundToInt(this.transform.position.x),
            Mathf.RoundToInt(this.transform.position.y),
            Mathf.RoundToInt(this.transform.position.z)
        );
        this.transform.position = position;
    }

    private void SnapToGrid2()
    {
        Vector3Int position = new Vector3Int(
            Mathf.RoundToInt(this.transform.position.x),
            Mathf.RoundToInt(this.transform.position.y),
            Mathf.RoundToInt(this.transform.position.z)
        );
        this.transform.position = position;

        string xStr = ZeroPrependStr(position.x.ToString());
        string yStr = ZeroPrependStr(position.y.ToString());
        string zStr = ZeroPrependStr(position.z.ToString());

        this.name = this.tag + "(" + xStr + "," + yStr + "," + zStr + ")";
    }

    private string ZeroPrependStr(string str)
    {
        if(str.Length == 1)
        {
            return "0" + str;
        }
        return str;
    }
}
#endif

