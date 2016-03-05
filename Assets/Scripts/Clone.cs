using UnityEngine;
using System.Collections;

public class Clone : MonoBehaviour
{
    // Globals
    private Globals globals;
    // Transform history
    public History History;

    // Initialization
    void Start()
    {
        // Initialize variables
        this.globals = GameObject.Find("globals").GetComponent<Globals>();

        // Get player
        PlayerController player = this.globals.Player;
        // Get player bones
        Transform[] bones = player.GetBones(player.transform);
        // Get clone bones
        Transform[] bones2 = player.GetBones(transform);
        // Create history
        this.History = new History(bones, bones2);
        // Add to TCM
        this.globals.TCM.Histories.Add(this.History);

        // Debug:  Set color
        foreach (Transform bone in bones2)
        {
            if (bone.name.Substring(0, 11) == "playerClone") { continue; }
            SpriteRenderer sr = bone.gameObject.GetComponent<SpriteRenderer>();
            sr.color = new Color32(149, 165, 166, 255);
        }
    }
}
