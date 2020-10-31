using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Linq;

public class DropDownSteamFriends : MonoBehaviour
{
    Dropdown _dropdown;
    Dropdown Dropdown => _dropdown ? _dropdown : _dropdown = GetComponent<Dropdown>();

    [System.Serializable] public class UnityEventULong : UnityEngine.Events.UnityEvent<ulong> { }
    [System.Serializable] public class UnityEventString : UnityEngine.Events.UnityEvent<string> { }

    public UnityEventULong onFriendSelectedId;
    public UnityEventString onFriendSelectedId_string;
    public UnityEventString onFriendSelectedName;

    void Start()
    {
        if (Dropdown)
        {
            StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
                () => SteamClient.IsLoggedOn,
                UpdateList
            ));

            Dropdown.onValueChanged.AddListener((val) =>
           {
               if (friendsList != null && val < friendsList.Length)
               {
                   selectedFriend = friendsList[val];
                   onFriendSelectedId?.Invoke(selectedFriend.Id.Value);
                   onFriendSelectedId_string?.Invoke(selectedFriend.Id.Value.ToString());
                   onFriendSelectedName?.Invoke(selectedFriend.Name);
               }
           });
        }
    }

    Friend selectedFriend;
    Friend[] friendsList;

    void UpdateList()
    {
        Dropdown.ClearOptions();

        friendsList = SteamFriends.GetFriends().ToArray();

        Dropdown.AddOptions(
            friendsList.Select(f => new Dropdown.OptionData(f.Name))
            .ToList()
        );
    }
}
