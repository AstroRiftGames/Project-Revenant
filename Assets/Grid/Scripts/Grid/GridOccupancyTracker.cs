using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RoomGrid))]
public class GridOccupancyTracker : MonoBehaviour
{
    private readonly Dictionary<IGridOccupant, Vector3Int> _cellsByOccupant = new();
    private readonly Dictionary<Vector3Int, HashSet<IGridOccupant>> _occupantsByCell = new();
    private readonly Dictionary<Object, PersistentGridOccupant> _persistentOccupantsByOwner = new();
    
    private readonly Dictionary<IGridOccupant, Vector3Int> _reservedCellsByOccupant = new();
    private readonly Dictionary<Vector3Int, IGridOccupant> _cellReservations = new();

    private RoomGrid _grid;

    private void Awake()
    {
        ResolveGrid();
    }

    public void RegisterOccupant(IGridOccupant occupant)
    {
        if (occupant == null)
            return;

        ResolveGrid();
        if (_grid == null || !occupant.OccupiesCell)
        {
            ReleaseOccupant(occupant);
            return;
        }

        RegisterOccupant(occupant, _grid.WorldToCell(occupant.OccupancyWorldPosition));
    }

    public void RegisterOccupant(IGridOccupant occupant, Vector3Int cell)
    {
        if (occupant == null)
            return;

        if (!occupant.OccupiesCell)
        {
            ReleaseOccupant(occupant);
            return;
        }

        if (_cellsByOccupant.TryGetValue(occupant, out Vector3Int previousCell) && previousCell == cell)
            return;

        RemoveFromCurrentCell(occupant);
        _cellsByOccupant[occupant] = cell;

        if (!_occupantsByCell.TryGetValue(cell, out HashSet<IGridOccupant> occupants))
        {
            occupants = new HashSet<IGridOccupant>();
            _occupantsByCell[cell] = occupants;
        }

        occupants.Add(occupant);
    }

    public void MoveOccupant(IGridOccupant occupant, Vector3Int destinationCell)
    {
        if (_cellReservations.TryGetValue(destinationCell, out IGridOccupant reservedBy) && 
            !ReferenceEquals(reservedBy, occupant))
        {
            Debug.LogWarning($"[GridOccupancyTracker] Cannot move {occupant} to {destinationCell}: cell is reserved by {reservedBy}");
            return;
        }
        
        ReleaseReservation(occupant);
        
        RegisterOccupant(occupant, destinationCell);
    }
    
    public bool TryReserveCell(Vector3Int cell, IGridOccupant occupant)
    {
        if (cell == Vector3Int.zero || occupant == null)
            return false;
        
        if (_cellReservations.ContainsKey(cell))
            return false;
        
        if (IsOccupied(cell, occupant))
            return false;
        
        _reservedCellsByOccupant[occupant] = cell;
        _cellReservations[cell] = occupant;
        
        return true;
    }
    
    public void ReleaseReservation(IGridOccupant occupant)
    {
        if (occupant == null)
            return;
        
        if (!_reservedCellsByOccupant.TryGetValue(occupant, out Vector3Int reservedCell))
            return;
        
        _reservedCellsByOccupant.Remove(occupant);
        _cellReservations.Remove(reservedCell);
    }
    
    public bool IsCellReserved(Vector3Int cell, IGridOccupant ignoredOccupant = null)
    {
        if (!_cellReservations.TryGetValue(cell, out IGridOccupant reservingOccupant))
            return false;
        
        if (ignoredOccupant != null && ReferenceEquals(reservingOccupant, ignoredOccupant))
            return false;
        
        return true;
    }
    
    public bool IsCellBlockedFor(IGridOccupant occupant, Vector3Int cell)
    {
        return IsOccupied(cell, occupant) || IsCellReserved(cell, occupant);
    }

    public void ReleaseOccupant(IGridOccupant occupant)
    {
        if (occupant == null)
            return;

        RemoveFromCurrentCell(occupant);
    }

    public void RegisterPersistentBlocker(Object owner, Vector3Int cell)
    {
        if (owner == null)
            return;

        ReleasePersistentBlocker(owner);

        var blocker = new PersistentGridOccupant(cell);
        _persistentOccupantsByOwner[owner] = blocker;
        RegisterOccupant(blocker, cell);
    }

    public void ReleasePersistentBlocker(Object owner)
    {
        if (owner == null)
            return;

        if (!_persistentOccupantsByOwner.TryGetValue(owner, out PersistentGridOccupant blocker))
            return;

        _persistentOccupantsByOwner.Remove(owner);
        ReleaseOccupant(blocker);
    }

    public bool IsOccupied(Vector3Int cell, IGridOccupant ignoredOccupant = null)
    {
        if (!_occupantsByCell.TryGetValue(cell, out HashSet<IGridOccupant> occupants))
            return false;

        foreach (IGridOccupant occupant in occupants)
        {
            if (occupant == null || ReferenceEquals(occupant, ignoredOccupant) || !occupant.OccupiesCell)
                continue;

            return true;
        }

        return false;
    }

    public bool DoesCellBlockMovement(Vector3Int cell, IGridOccupant ignoredOccupant = null)
    {
        if (_occupantsByCell.TryGetValue(cell, out HashSet<IGridOccupant> occupants))
        {
            foreach (IGridOccupant occupant in occupants)
            {
                if (occupant == null || ReferenceEquals(occupant, ignoredOccupant) || !occupant.OccupiesCell || !occupant.BlocksMovement)
                    continue;

                return true;
            }
        }
        
        if (IsCellReserved(cell, ignoredOccupant))
            return true;

        return false;
    }

    public IGridOccupant GetBlockingOccupant(Vector3Int cell, IGridOccupant ignoredOccupant = null)
    {
        if (_occupantsByCell.TryGetValue(cell, out HashSet<IGridOccupant> occupants))
        {
            foreach (IGridOccupant occupant in occupants)
            {
                if (occupant == null || ReferenceEquals(occupant, ignoredOccupant) || !occupant.OccupiesCell || !occupant.BlocksMovement)
                    continue;

                return occupant;
            }
        }
        
        return null;
    }

    public IGridOccupant GetReservingOccupant(Vector3Int cell)
    {
        return _cellReservations.TryGetValue(cell, out IGridOccupant occupant) ? occupant : null;
    }

    private void RemoveFromCurrentCell(IGridOccupant occupant)
    {
        if (!_cellsByOccupant.TryGetValue(occupant, out Vector3Int currentCell))
            return;

        _cellsByOccupant.Remove(occupant);

        if (!_occupantsByCell.TryGetValue(currentCell, out HashSet<IGridOccupant> occupants))
            return;

        occupants.Remove(occupant);
        if (occupants.Count == 0)
            _occupantsByCell.Remove(currentCell);
    }

    private void ResolveGrid()
    {
        if (_grid != null)
            return;

        _grid = GetComponent<RoomGrid>();

        if (_grid == null)
        {
            Debug.LogError(
                $"[GridOccupancyTracker] '{name}' requiere un RoomGrid en el mismo GameObject.",
                this);
        }
    }

    private sealed class PersistentGridOccupant : IGridOccupant
    {
        private readonly Vector3Int _cell;

        public PersistentGridOccupant(Vector3Int cell)
        {
            _cell = cell;
        }

        public Vector3 OccupancyWorldPosition => new Vector3(_cell.x, _cell.y, 0f);
        public bool OccupiesCell => true;
        public bool BlocksMovement => true;
    }
}
