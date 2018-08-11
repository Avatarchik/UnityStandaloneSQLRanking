using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour {

    private RankingSQL rankSQL;

    private string currentInsertedID = "";

    // Use this for initialization
    void Start () {
        rankSQL = new RankingSQL();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnAddScore()
    {
        int val = (int)Random.Range(0, 1000000f);
        string name = "testname" + val.ToString();
        string nowRank = rankSQL.InsertScore(name, val);
        Debug.Log("Current Score: "+nowRank);
        currentInsertedID = rankSQL.GetCurrentInsertedID();
    }

    public void OnDeleteScore()
    {
        if (string.IsNullOrEmpty(currentInsertedID))
        {
            return;
        }

        rankSQL.DeleteScore(int.Parse(currentInsertedID));
    }

    public void OnDeleteAllRecords()
    {
        rankSQL.DeleteAllScores();
    }

    public void OnUpdateScore()
    {
        if (string.IsNullOrEmpty(currentInsertedID))
        {
            return;
        }

        int val = (int)Random.Range(0, 1000000f);
        rankSQL.UpdateScore(int.Parse(currentInsertedID), val);
    }

    public void OnGetScore()
    {
        rankSQL.GetHighScores(20);
    }

    public void OnOutCSV()
    {
        rankSQL.OutCSV(rankSQL.GetFilePath() + "/ranking.csv");
    }
}
