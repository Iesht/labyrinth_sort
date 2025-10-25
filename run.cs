using System;
using System.Collections.Generic;

namespace LabyrinthSort;

public class Program
{
    private static readonly int[] RoomEntrances = [2, 4, 6, 8];
    private static readonly Dictionary<char, int> Energy = new()
    {
        ['A'] = 1,
        ['B'] = 10,
        ['C'] = 100,
        ['D'] = 1000
    };

    private class State(char[] corridor, char[][] rooms)
    {
        public char[] Corridor { get; } = corridor;
        public char[][] Rooms { get; } = rooms;
        public int RoomSize { get; } = rooms[0].Length;

        public bool IsGoalState()
        {
            if (Corridor.Any(c => c != '.'))
                return false;
            
            for (var i = 0; i < Rooms.Length; i++)
            {
                if (Rooms[i].Any(c => c != (char)('A' + i))) 
                    return false;
            }
            return true;
        }

        public State Copy()
        {
            var corridor = new char[Corridor.Length];
            Array.Copy(Corridor, corridor, Corridor.Length);
            
            var rooms = new char[Rooms.Length][];
            for (var i = 0; i < Rooms.Length; i++)
            {
                rooms[i] = new char[Rooms[i].Length];
                Array.Copy(Rooms[i], rooms[i], Rooms[i].Length);
            }
            return new State(corridor, rooms);
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is not State other)
                return false;
            if (!Corridor.SequenceEqual(other.Corridor))
                return false;
            for (var i = 0; i < Rooms.Length; i++)
            {
                if (!Rooms[i].SequenceEqual(other.Rooms[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Corridor.Aggregate(17, (cur, c) => cur * 31 + c.GetHashCode());

                foreach (var room in Rooms) 
                    hash = room.Aggregate(hash, (cur, c) => cur * 31 + c.GetHashCode());

                return hash;
            }
        }
    }

    private static IEnumerable<(State next, int energy)> GetNextStates(State curState)
    {
        foreach (var state in GetNextStatesFromRooms(curState))
            yield return state;

        foreach (var state in GetNextStatesToRooms(curState))
            yield return state;
    }

    private static IEnumerable<(State next, int energy)> GetNextStatesFromRooms(State state)
    {
        for (var r = 0; r < state.Rooms.Length; r++)
        {
            var entrance = RoomEntrances[r];
            var room = state.Rooms[r];
            var roomSize = state.RoomSize;

            var topRoomObjectInd = 0;
            while (topRoomObjectInd < roomSize && room[topRoomObjectInd] == '.')
                topRoomObjectInd++;
            
            if (topRoomObjectInd == roomSize)
                continue;
            
            var roomObject = room[topRoomObjectInd];
            var roomObjectType = roomObject - 'A';
            
            if (r == roomObjectType && room.Skip(topRoomObjectInd).All(c => c == roomObject))
                continue;

            foreach (var pos in EnumerateFreePositions(state, entrance))
            {
                var nextState = state.Copy();
                nextState.Rooms[r][topRoomObjectInd] = '.';
                nextState.Corridor[pos] = roomObject;
                
                var steps = topRoomObjectInd + 1 + Math.Abs(pos - entrance);
                yield return (nextState, steps * Energy[roomObject]);
            }
        }
    }

    private static IEnumerable<(State next, int energy)> GetNextStatesToRooms(State state)
    {
        for (var c = 0; c < state.Corridor.Length; c++)
        {
            var roomObject = state.Corridor[c];
            if (roomObject == '.')
                continue;
            
            var roomObjectType =  roomObject - 'A';
            var targetEntrance = RoomEntrances[roomObjectType];

            var direction = targetEntrance > c ? 1 : -1;
            if (IsWayBlocked(state.Corridor, c, targetEntrance, direction))
                continue;

            var targetRoom = state.Rooms[roomObjectType];
            if (targetRoom.Any(ch => ch != '.' && ch != roomObject))
                continue;

            var targetPos = targetRoom.Length - 1;
            while (targetPos >= 0 && targetRoom[targetPos] != '.')
                targetPos--;
            if (targetPos < 0)
                continue;

            var nextState = state.Copy();
            nextState.Corridor[c] = '.';
            nextState.Rooms[roomObjectType][targetPos] = roomObject;
            
            var steps = targetPos + 1 + Math.Abs(c - targetEntrance);
            yield return (nextState, steps * Energy[roomObject]);
        }
    }

    private static IEnumerable<int> EnumerateFreePositions(State state, int start)
    {
        foreach (var direction in new[] { -1, 1 })
        {
            for (var i = start + direction; i >= 0 && i < state.Corridor.Length; i += direction)
            {
                if (state.Corridor[i] != '.') break;
                if (RoomEntrances.Contains(i)) continue;
                yield return i;
            }
        }
    }
    
    private static bool IsWayBlocked(char[] corridor, int from, int to, int direction)
    {
        for (var i = from + direction; i != to; i += direction)
        {
            if (corridor[i] != '.')
                return true; 
        }
        return false;
    }

    private static State ParseInputToState(List<string> lines)
    {
        var corridor = lines[1].Trim('#').ToCharArray();
        var roomLines = lines.Skip(2).Take(lines.Count - 3).ToArray();
        var roomSize = roomLines.Length;
        var rooms = new char[4][];
        for (var i = 0; i < 4; i++)
        {
            rooms[i] = new char[roomSize];
            for (var j = 0; j < roomSize; j++)
                rooms[i][j] = roomLines[j][3 + 2 * i];
        }
        
        return new State(corridor, rooms);
    }
    
    public static int Solve(List<string> lines)
    {
        var start =  ParseInputToState(lines);
        
        var queue = new PriorityQueue<State, int>();
        var dist = new Dictionary<State, int> { [start] = 0 };
        queue.Enqueue(start, 0);

        while (queue.TryDequeue(out var curState, out var curEnergy))
        {
            if (curState.IsGoalState())
                return curEnergy;
            if (curEnergy > dist[curState])
                continue;
            
            foreach (var (next, energy) in GetNextStates(curState))
            {
                var newEnergy = energy + curEnergy;
                if (!dist.TryGetValue(next, out var oldEnergy) || oldEnergy > newEnergy)
                {
                    dist[next] = newEnergy;
                    queue.Enqueue(next, newEnergy);
                }
            }
        }

        return -1;
    }

    public static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null)
        {
            lines.Add(line);
        }

        int result = Solve(lines);
        Console.WriteLine(result);
    }
}
