/*
 * GArray
 *
 * From https://gist.github.com/mjs3339/c5c26638a7485afaaed6ed171f1bfb0d
 */

using System;
using System.Collections.Generic;

[Serializable]
public class GArray<T> {
    private readonly int _capacity = 4096;
    private int _count;
    private int _position;
    private T[] Thing;
    public GArray() {
        _capacity = 4096;
        _position = 0;
        _count = 0;
        Thing = new T[_capacity];
    }
    public GArray(IEnumerable<T> collection) {
        if (collection == null) {
            throw new Exception("Collection cannot be null.");
        }
        var Collection = collection as T[];
        var count = Collection.Length;
        _capacity = 32;
        _position = 0;
        _count = count;
        Thing = new T[count];
        Array.Copy(Collection, 0, Thing, 0, count);
    }
    public bool ReadEnd => _position == Thing.Length;
    public void Write(IEnumerable<T> collection) {
        if (collection == null) {
            throw new Exception("Collection cannot be null.");
        }
        var Collection = collection as T[];
        var count = Collection.Length;
        EnsureSize(_count + count);
        Array.Copy(Collection, 0, Thing, _count, count);
        _count += count;
    }
    public void Write(T value) {
        EnsureSize(_count + 1);
        Thing[_count] = value;
        _count++;
    }
    /// <summary>
    ///     Read an array of items from the main array starting from read position of count items.
    /// </summary>
    public T[] Read(int count) {
        if (_position + count > Thing.Length - 1)
            throw new ArgumentException("Position out of range.");
        var tarray = new T[count];
        Array.Copy(Thing, _position, tarray, 0, count);
        _position += count;
        return tarray;
    }
    /// <summary>
    ///     Reads one items from the array
    /// </summary>
    public T Read() {
        if (_position > Thing.Length - 1)
            throw new ArgumentException("Position out of range.");
        return Thing[_position++];
    }
    private void EnsureSize(int MinimumSize) {
        if (Thing.Length >= MinimumSize) return;
        var NewLength = Thing.Length == 0 ? 4096 : Thing.Length * 2;
        if (NewLength < MinimumSize) NewLength = MinimumSize;
        var newtArray = new T[NewLength];
        Array.Copy(Thing, 0, newtArray, 0, _count);
        Thing = newtArray;
    }
    /// <summary>
    ///     Returns an array of type T items.
    /// </summary>
    public T[] ToArray() {
        var objArray = new T[_count];
        Array.Copy(Thing, 0, objArray, 0, _count);
        return objArray;
    }
}