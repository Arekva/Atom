﻿using System.Collections.Concurrent;
using System.Diagnostics;

namespace Atom.Engine;

public abstract class AtomObject : IDeletable, IEquatable<AtomObject>
{
    private static ConcurrentDictionary<Guid, AtomObject> _objects = new ();
    public static IEnumerable<AtomObject> Objects => _objects.Values;
    
    
    public Guid GUID { get; }

    private volatile bool _isDeleted;
    public bool IsDeleted => _isDeleted;

    private readonly object _deletionLock = new();

    private volatile string? _name;
    public string? Name
    {
        get => _name;
        set
        {
            ThrowIfDeleted();
            _name = value;
        }
    }

    public AtomObject(string? name = "Unnamed")
    {
        GUID = Guid.NewGuid();
        _name = name;
        
        _objects.TryAdd(GUID, this);
    }

    protected void ThrowDeleted() => throw new ObjectDeletedException($"Cannot access {new StackFrame(1).GetMethod()!.Name}: object is deleted.");
    protected void ThrowIfDeleted() { if (_isDeleted) throw new ObjectDeletedException($"Cannot access {new StackFrame(1).GetMethod()!.Name}: object is deleted."); }

    public bool Equals(AtomObject? other) => other is not null && GUID.Equals(other.GUID);
    public override bool Equals(object? other) => other is AtomObject bo && Equals(bo);

    public override string ToString() => $"{_name} ({GetType().Name})"; 

    public override int GetHashCode() => GUID.GetHashCode();

    public void Dispose() => Delete();
    
    ~AtomObject() => Dispose();


    protected internal virtual void Frame() { }
    
    protected internal virtual void Render() { }
    
    protected internal virtual void PhysicsFrame() { }
    
    
    public virtual void Delete()
    {
        lock (_deletionLock)
        {
            if (_isDeleted)
            {
                ThrowDeleted();
            }
            _isDeleted = true;
            _objects.TryRemove(GUID, out _);
            GC.SuppressFinalize(this);
        }
    }
}
