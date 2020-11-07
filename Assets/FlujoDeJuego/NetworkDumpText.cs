using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class NetworkDumpText : MonoBehaviour
{
    Text _uiText;
    Text UiText => _uiText?_uiText:_uiText=GetComponent<Text>();

    void Update() {
        if (UiText) {
            UiText.text = "";
            UiText.text += $"NetworkTime.offset = {NetworkTime.offset*1000:0}\n";
            UiText.text += $"NetworkTime.rtt = {NetworkTime.rtt*1000:0}\n";
            UiText.text += $"NetworkTime.rttSd = {NetworkTime.rttSd*1000:0}\n";
            UiText.text += $"NetworkTime.rttVar = {NetworkTime.rttVar*1000:0}\n";
            UiText.text += $"NetworkTime.time = {NetworkTime.time*1000:0}\n";
            UiText.text += $"NetworkTime.timeSd = {NetworkTime.timeSd*1000:0}\n";
            UiText.text += $"NetworkTime.timeVar = {NetworkTime.timeVar*1000:0}\n";
        }
    }
}
