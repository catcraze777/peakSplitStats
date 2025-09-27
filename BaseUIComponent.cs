using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SplitsStats;

public abstract class BaseUIComponent : MonoBehaviour, IComparable<BaseUIComponent>
{
    /// <summary>
    /// The position to put this component in the SplitsStats UI, either UIComponentPosition.TopLeft or UIComponentPosition.TopRight.
    /// </summary>
    public UIComponentPosition uiPosition;

    /// <summary>
    /// The RectTransform of this component's gameObject.
    /// </summary>
    public RectTransform rectTransform => GetRectTransform();

    /// <summary>
    /// A default height for the internal scale of UI elements. The component's gameObject is scaled to change the vertical height of the element.
    /// </summary>
    public const float INITIAL_HEIGHT = 100f;

    /// <summary>
    /// A simpler name for <c>CreateBaseUIComponent</c>
    /// </summary>
    public static T CreateUIComponent<T>(string name, Transform parent = null) where T : BaseUIComponent
    {
        return CreateBaseUIComponent<T>(name, parent);
    }

    /// <summary>
    /// Create and return a BaseUIComponent attached to a new gameObject parented to a provided transform. Use a template to allow subclasses to reuse object construction in the superclass.
    /// </summary>
    /// <typeparam name="T">BaseUIComponent or one of its defined subclasses.</typeparam>
    /// <param name="name">The name to use for this component's gameObject.</param>
    /// <param name="parent">The parent transform to set as this component's gameObject's parent. Setting to null creates the object as a scene root object.</param>
    /// <returns>A BaseUIComponent attached to a newly created gameObject. Matches type of template since BaseUIComponent is abstract.</returns>
    public static T CreateBaseUIComponent<T>(string name, Transform parent = null) where T : BaseUIComponent
    {
        GameObject baseObject = new GameObject(name, typeof(RectTransform), typeof(T));
        T currComponent = baseObject.GetComponent<T>();
        if (parent != null) baseObject.transform.parent = parent;

        return currComponent;
    }
    public BaseUIComponent()
    {
        uiPosition = UIComponentPosition.TopRight;
        _priority = 0;
    }

    /// <summary>
    /// The Monobehavior Start Script: This is responsible for creating and modifying child gameObjects needed for the UI component, if they don't already exist.
    /// </summary>
    public virtual void Start()
    {
        rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Change the height of the UI object. Ensure when overriding that this always matches the pixel height of the entire element, otherwise rendered components will overlap each other.
    /// </summary>
    /// <param name="newSize"> The new height of the object. </param>
    public virtual void SetHeight(float newSize)
    {
        this.transform.localScale = new Vector3(newSize / INITIAL_HEIGHT, newSize / INITIAL_HEIGHT, 1.0f);
    }

    /// <summary>
    /// Get the height of the UI object. Ensure when overriding that this always matches the pixel height of the entire element, otherwise rendered components will overlap each other.
    /// </summary>
    /// <returns> The height of the object. </returns>
    public virtual float GetHeight()
    {
        return this.transform.localScale.y * INITIAL_HEIGHT;
    }

    /// <summary>
    /// Get the RectTransform associated with this component.
    /// </summary>
    public virtual RectTransform GetRectTransform()
    {
        return gameObject.GetComponent<RectTransform>();
    }

    private int _priority;
    /// <summary>
    /// Set the priority of ordering for all InfoComponents displayed by a SplitsManager object. Lower priorities result in being placed higher in the rendered stats.
    /// </summary>
    /// <param name="priority">The integer used for sorting InfoComponents.</param>
    public void SetSortingPriority(int priority)
    {
        inInfoList?.Reorder(this);
        _priority = priority;
    }

    internal BaseUIComponentList inInfoList = null;

    public int CompareTo(BaseUIComponent other)
    {
        return this._priority - other._priority;
    }
}

public class BaseUIComponentList : IList<BaseUIComponent>
{
    // THIS LIST SHOULD ALWAYS BE SORTED!!!!!
    private List<BaseUIComponent> _baseUIComponents;

    public bool IsFixedSize => false;

    public bool IsReadOnly => false;

    public int Count => _baseUIComponents.Count;

    public bool IsSynchronized => false;

    public BaseUIComponent SyncRoot => throw new NotImplementedException();

    public BaseUIComponent this[int index] { get => _baseUIComponents[index]; set => throw new NotSupportedException("Setting via the indexor is not supported to maintain a sorted order of elements! Use the Delete(BaseUIComponent original) then Add(BaseUIComponent value) method instead!"); }

    public BaseUIComponentList()
    {
        _baseUIComponents = [];
    }

    internal void Reorder(BaseUIComponent element)
    {
        if (!Contains(element)) return;
        Remove(element);
        Add(element);
    }

    /// <summary>
    /// Insert a component into this list while keeping elements sorted by priority (ascending order)
    /// </summary>
    /// <param name="value">The component to insert into the list.</param>
    public void Add(BaseUIComponent value)
    {
        value.inInfoList = this;
        int insertionIndex = _baseUIComponents.BinarySearch(value);
        if (insertionIndex >= 0)
        {
            if (_baseUIComponents[insertionIndex] == value) return;
            while (insertionIndex < Count && _baseUIComponents[insertionIndex].CompareTo(value) == 0) insertionIndex++;
        }
        else insertionIndex = ~insertionIndex;
        _baseUIComponents.Insert(insertionIndex, value);
    }

    public void Clear()
    {
        _baseUIComponents.Clear();
    }

    public bool Contains(BaseUIComponent value)
    {
        return _baseUIComponents.Contains(value);
    }

    public int IndexOf(BaseUIComponent value)
    {
        return _baseUIComponents.IndexOf(value);
    }

    public void Insert(int index, BaseUIComponent value)
    {
        throw new NotSupportedException("Inserting is not supported to maintain a sorted order of elements! Use the Add(BaseUIComponent value) method instead!");
    }

    public bool Remove(BaseUIComponent item)
    {
        bool removeSuccessful = _baseUIComponents.Remove(item);
        if (removeSuccessful) item.inInfoList = null;
        return removeSuccessful;
    }

    public void RemoveAt(int index)
    {
        _baseUIComponents[index].inInfoList = null;
        _baseUIComponents.RemoveAt(index);
    }

    public void CopyTo(BaseUIComponent[] array, int arrayIndex)
    {
        _baseUIComponents.CopyTo(array, arrayIndex);
    }

    public IEnumerator GetEnumerator()
    {
        return _baseUIComponents.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _baseUIComponents.GetEnumerator();
    }

    IEnumerator<BaseUIComponent> IEnumerable<BaseUIComponent>.GetEnumerator()
    {
        return _baseUIComponents.GetEnumerator();
    }
}

public enum UIComponentPosition
{
    TopLeft,
    TopRight
}