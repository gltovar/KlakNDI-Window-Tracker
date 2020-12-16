using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TestWindowTracking : MonoBehaviour
{
    NDITrackDesktopWindow trackDW;
    Material mat;

    // Start is called before the first frame update
    void Start()
    {
        if (!mat)
        {
            mat = GetComponent<Renderer>().material;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (trackDW)
        {
            mat.SetTextureOffset("_MainTex", new Vector2(trackDW.normalizedWindowRectangle.xMin, trackDW.normalizedWindowRectangle.yMin));
            mat.SetTextureScale("_MainTex", new Vector2(trackDW.normalizedWindowRectangle.width, trackDW.normalizedWindowRectangle.height));
        }
    }
}
