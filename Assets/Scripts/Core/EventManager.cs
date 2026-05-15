using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    private Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Subscribe(string eventName, Action listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] += listener;
        }
        else
        {
            eventDictionary.Add(eventName, listener);
        }
    }

    public void Unsubscribe(string eventName, Action listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] -= listener;
        }
    }

    public void TriggerEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (eventDictionary.TryGetValue(eventName, out Action thisEvent))
        {
            Debug.Log($"[EventManager] Triggering Event: {eventName}");
            thisEvent?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[EventManager] Event not found: {eventName}");
        }
    }
}
