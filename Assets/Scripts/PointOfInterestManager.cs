using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using i5.LLM_AR_Tourguide.Configurations;
using i5.LLM_AR_Tourguide.DataSources;
using i5.LLM_AR_Tourguide.TourGeneration;
using UnityEngine;

public class PointOfInterestManager : MonoBehaviour
{
    public PointOfInterestData poiData;
    private InformationController _informationController;
    private SaveDataManager _saveDataManager;

    public void Start()
    {
        _informationController = GetComponent<InformationController>();
        _saveDataManager = GetComponent<SaveDataManager>();
        DebugEditor.Log("PointOfInterestManager " + poiData.pointOfInterests.Length);
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
        {
            var poi = poiData.pointOfInterests[i];
            poi.gameObjectLocation = new GameObject();
            poi.gameObjectLocation.name = poi.title;
            poi.isCompletedVisit = false;
            poi.isPersonalized = false;


            for (var j = 0; j < poi.subPointOfInterests.Count; j++)
            {
                var tempSubPoi = poi.subPointOfInterests[j];
                tempSubPoi.gameObjectLocation = new GameObject();
                tempSubPoi.gameObjectLocation.name = tempSubPoi.title;
                poi.subPointOfInterests[j] = tempSubPoi;
            }

            poiData.pointOfInterests[i] = poi;
        }


        WikipediaAPI.GetInformationForAllPOIs();
    }


    public void setAllPOIDescriptions(string[] descriptions)
    {
        //Remove whitespace from descriptions
        for (var i = 0; i < descriptions.Length; i++)
        {
            descriptions[i] = descriptions[i].Trim();
            descriptions[i] = Regex.Replace(descriptions[i], @"\n+", "\n");
            descriptions[i] = Regex.Replace(descriptions[i], @"\t+", "");
        }

        if (descriptions.Length != poiData.pointOfInterests.Length)
            throw new Exception("The number of descriptions (" + descriptions.Length +
                                ") does not match the number of POIs (" + poiData.pointOfInterests.Length + ")");
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
            if (!string.IsNullOrWhiteSpace(descriptions[i]))
                poiData.pointOfInterests[i].information = descriptions[i];
    }

    public void setAllSubPOIDescriptions(string[] descriptions, int index)
    {
        //Remove whitespace from descriptions
        for (var i = 0; i < descriptions.Length; i++)
        {
            descriptions[i] = descriptions[i].Trim();
            descriptions[i] = Regex.Replace(descriptions[i], @"\n+", "\n");
            descriptions[i] = Regex.Replace(descriptions[i], @"\t+", "");
        }

        if (descriptions.Length != poiData.pointOfInterests[index].subPointOfInterests.Count)
            throw new Exception("The number of descriptions (" + descriptions.Length +
                                ") does not match the number of Sub POIs (" + poiData.pointOfInterests.Length + ")");
        for (var i = 0; i < poiData.pointOfInterests[index].subPointOfInterests.Count; i++)
            if (!string.IsNullOrWhiteSpace(descriptions[i]))
            {
                var tempSubPoi = poiData.pointOfInterests[index].subPointOfInterests[i];
                tempSubPoi.description = descriptions[i];
                poiData.pointOfInterests[index].subPointOfInterests[i] = tempSubPoi;
            }
    }

    [ContextMenu("RegenerateTourContent")]
    public void DeleteAllTourContent()
    {
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
        {
            var poi = poiData.pointOfInterests[i];
            poi.tourContent = null;
            poiData.pointOfInterests[i] = poi;
        }

        _saveDataManager.SavePointOfInterestManager();
    }

    public string[] getAllPOITitlesAsString()
    {
        var allPOIs = new string[poiData.pointOfInterests.Length];
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
            allPOIs[i] = poiData.pointOfInterests[i].title;
        return allPOIs;
    }

    public string[] getAllSubPOITitlesAsString(int index)
    {
        var allPOIs = new string[poiData.pointOfInterests[index].subPointOfInterests.Count];
        for (var i = 0; i < poiData.pointOfInterests[index].subPointOfInterests.Count; i++)
            allPOIs[i] = poiData.pointOfInterests[index].subPointOfInterests[i].sourceTitle;
        return allPOIs;
    }

    public List<string> getAllPOIAndActiveSubPoiTitlesAsString()
    {
        var allPOIs = new List<string>();
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
        {
            allPOIs.Add(poiData.pointOfInterests[i].title);
            for (var j = 0; j < poiData.pointOfInterests[i].subPointOfInterests.Count; j++)
                if (poiData.pointOfInterests[i].subPointOfInterests[j].gameObjectLocation.gameObject
                        .activeInHierarchy && poiData.pointOfInterests[i].subPointOfInterests[j].gameObjectLocation
                        .gameObject.activeSelf)
                    allPOIs.Add(poiData.pointOfInterests[i].subPointOfInterests[j].title);
        }

        return allPOIs;
    }

    public string getAllPOIsAsString()
    {
        var allPOIs = "";
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
            if (allPOIs == "")
                allPOIs = poiData.pointOfInterests[i].title;
            else
                allPOIs += ", " + poiData.pointOfInterests[i].title;

        return allPOIs;
    }

    public string getVisitedPOIs()
    {
        var visitedPOIs = "";
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
        {
            if (i == _informationController.CurrentInformationIndex)
                continue;
            if (poiData.pointOfInterests[i].isCompletedVisit)
            {
                if (visitedPOIs == "")
                    visitedPOIs = poiData.pointOfInterests[i].title;
                else
                    visitedPOIs += ", " + poiData.pointOfInterests[i].title;
            }
        }

        return visitedPOIs;
    }

    public void SetTourContent(int index, TourContent[] tourContent)
    {
        poiData.pointOfInterests[index].tourContent = tourContent;
        _saveDataManager.SavePointOfInterestManager();
    }

    public PointOfInterestData.POI[] getAllPointOfInterests()
    {
        return poiData.pointOfInterests;
    }

    public PointOfInterestData.POI getCurrentPOI()
    {
        return poiData.pointOfInterests[_informationController.CurrentInformationIndex];
    }

    public PointOfInterestData.POI? getNextPOI()
    {
        if (poiData.pointOfInterests.Length == 0)
            return null;
        if (_informationController.CurrentInformationIndex < poiData.pointOfInterests.Length - 1)
            return poiData.pointOfInterests[_informationController.CurrentInformationIndex + 1];
        return null;
    }

    public int getSubPOICount()
    {
        var poi = poiData.pointOfInterests[_informationController.CurrentInformationIndex];
        return poi.subPointOfInterests.Count;
    }

    public string getUpcomingPOIs()
    {
        var uncompletedPOIs = "";
        for (var i = 0; i < poiData.pointOfInterests.Length; i++)
        {
            if (i == _informationController.CurrentInformationIndex)
                continue;
            if (!poiData.pointOfInterests[i].isCompletedVisit)
            {
                if (uncompletedPOIs == "")
                    uncompletedPOIs = poiData.pointOfInterests[i].title;
                else
                    uncompletedPOIs += ", " + poiData.pointOfInterests[i].title;
            }
        }

        return uncompletedPOIs;
    }

    public Transform GetPointOfInterestTransformByTitle(string title)
    {
        foreach (var poi in poiData.pointOfInterests)
            if (poi.title == title)
                return poi.gameObjectLocation.transform;
            else
                foreach (var subPoi in poi.subPointOfInterests)
                    if (subPoi.title == title)
                        return subPoi.gameObjectLocation.transform;

        return null;
    }

    public void UpdateVisibilityOfSubPov(int indexOfVisiblePoi)
    {
        if (!poiData || poiData.pointOfInterests == null) return;

        foreach (var poi in poiData.pointOfInterests)
        {
            if (poi.subPointOfInterests == null) continue;

            foreach (var subPoi in poi.subPointOfInterests)
            {
                if (!subPoi.gameObjectLocation) continue;

                subPoi.gameObjectLocation.SetActive(false);
                if (subPoi.gameObjectLocation.transform.parent)
                    subPoi.gameObjectLocation.transform.parent.gameObject.SetActive(false);
            }
        }

        if (indexOfVisiblePoi < 0 || indexOfVisiblePoi >= poiData.pointOfInterests.Length) return;

        var visiblePoi = poiData.pointOfInterests[indexOfVisiblePoi];
        if (visiblePoi.subPointOfInterests == null) return;

        foreach (var subPoi in visiblePoi.subPointOfInterests)
        {
            if (!subPoi.gameObjectLocation) continue;

            subPoi.gameObjectLocation.SetActive(true);
            if (subPoi.gameObjectLocation.transform.parent)
                subPoi.gameObjectLocation.transform.parent.gameObject.SetActive(true);
        }
    }


    public string GetPointOfInterestInformationByTitle(string title)
    {
        foreach (var poi in poiData.pointOfInterests)
            if (poi.title == title)
                return poi.information;
            else
                foreach (var subPoi in poi.subPointOfInterests)
                    if (subPoi.title == title)
                        return subPoi.description;

        return null;
    }
}