using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RhythmHeavenMania.Editor.Track;

namespace RhythmHeavenMania.Editor
{
    public class EventParameterManager : MonoBehaviour
    {
        [Header("General References")]
        [SerializeField] private GameObject eventSelector;
        [SerializeField] private GridGameSelector gridGameSelector;

        [Header("Property Prefabs")]
        [SerializeField] private GameObject IntegerP;
        [SerializeField] private GameObject DropdownP;

        public Beatmap.Entity entity;

        private bool active;

        public static EventParameterManager instance { get; set; }

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!Timeline.instance.MouseInRectTransform(Editor.instance.eventSelectorBG) && active)
                {
                    Disable();
                }
            }
        }

        public void Disable()
        {
            active = false;
            eventSelector.SetActive(true);

            DestroyParams();
            Editor.instance.SetGameEventTitle($"Select game event for {gridGameSelector.SelectedMinigame}");
        }

        public void StartParams(Beatmap.Entity entity)
        {
            active = true;
            AddParams(entity);
        }

        private void AddParams(Beatmap.Entity entity)
        {
            var minigame = EventCaller.instance.GetMinigame(entity.datamodel.Split(0));
            int actionIndex = minigame.actions.IndexOf(minigame.actions.Find(c => c.actionName == entity.datamodel.Split(1)));
            Minigames.GameAction action = minigame.actions[actionIndex];

            if (action.parameters != null)
            {
                eventSelector.SetActive(false);
                this.entity = entity;

                Editor.instance.SetGameEventTitle($"Properties for {entity.datamodel}");

                DestroyParams();

                for (int i = 0; i < action.parameters.Count; i++)
                {
                    object param = action.parameters[i].parameter;
                    string caption = action.parameters[i].propertyCaption;
                    string propertyName = action.parameters[i].propertyName;

                    AddParam(propertyName, param, caption);
                }

                active = true;
            }
        }

        private void AddParam(string propertyName, object type, string caption)
        {
            GameObject prefab = IntegerP;

            if (type.GetType() == typeof(EntityTypes.Integer))
            {
                prefab = IntegerP;
            }
            else if (type.GetType() == typeof(RhythmHeavenMania.Util.EasingFunction.Ease))
            {
                prefab = DropdownP;
            }

            GameObject input = Instantiate(prefab);
            input.transform.SetParent(this.gameObject.transform);
            input.SetActive(true);
            input.transform.localScale = Vector2.one;

            var property = input.GetComponent<EventPropertyPrefab>();
            property.SetProperties(propertyName, type, caption);
        }

        private void DestroyParams()
        {
            active = false;
            for (int i = 2; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}