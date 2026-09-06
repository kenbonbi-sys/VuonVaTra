using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>Persistent hired NPCs. Work allocation is visual and never changes simulation slots.</summary>
    public sealed class WorkshopCrewView : MonoBehaviour
    {
        sealed class Member
        {
            public Transform Root;
            public CharacterView Walker;
            public WorkerWorkAnimation Work;
            public int Station;
            public Vector3 Home;
            public bool Visible;
            public bool Reserved;
            public Vector3[] Route;
            public int Waypoint;
        }

        StationView[] _stations;
        Member[] _members;
        bool[] _claimed;
        public int VisibleWorkerCount { get; private set; }

        public void Bind(StationView[] stations)
        {
            _stations = stations ?? new StationView[0];
            _members = new Member[_stations.Length];
            _claimed = new bool[_stations.Length];
            for (int i = 0; i < _stations.Length; i++)
            {
                var root = _stations[i] != null ? _stations[i].WorkerRoot : null;
                _members[i] = new Member
                {
                    Root = root, Station = i,
                    Home = root != null ? root.position : Vector3.zero,
                    Walker = root != null ? root.GetComponent<CharacterView>() : null,
                    Work = root != null ? root.GetComponent<WorkerWorkAnimation>() : null
                };
                if (root != null) root.gameObject.SetActive(false);
            }
        }

        public bool HasWorkerAt(string stageId)
        {
            if (_members == null) return false;
            foreach (var member in _members)
                if (member.Visible && member.Root != null && _stations[member.Station] != null &&
                    _stations[member.Station].StageId == stageId &&
                    (member.Root.position - WorkPoint(member.Station)).sqrMagnitude < 0.0225f)
                    return true;
            return false;
        }

        public void Render(GameState state)
        {
            if (_members == null) return;
            int hired = state == null ? 0 : Mathf.Clamp(state.HiredWorkers, 0, _members.Length);
            VisibleWorkerCount = 0;
            for (int i = 0; i < _members.Length; i++)
            {
                var member = _members[i];
                _claimed[i] = false;
                member.Reserved = false;
                if (member.Root == null) continue;
                bool visible = i < hired;
                if (visible != member.Visible)
                {
                    member.Visible = visible;
                    member.Root.gameObject.SetActive(visible);
                    if (!visible)
                    {
                        member.Station = i;
                        member.Route = null;
                        if (member.Walker != null) member.Walker.Teleport(member.Home);
                        else member.Root.position = member.Home;
                        if (member.Work != null) member.Work.SetWorking(false);
                    }
                }
                if (visible) VisibleWorkerCount++;
            }
            if (state == null) return;

            // Retain workers already on a job. A completed batch never hides or relocates them.
            foreach (var member in _members)
                if (member.Visible && Running(state, member.Station) && !_claimed[member.Station])
                {
                    member.Reserved = true;
                    _claimed[member.Station] = true;
                }

            for (int station = 0; station < _stations.Length; station++)
            {
                if (_claimed[station] || !Running(state, station)) continue;
                Member nearest = null;
                float distance = float.PositiveInfinity;
                foreach (var member in _members)
                {
                    if (!member.Visible || member.Root == null || member.Reserved) continue;
                    float candidate = (member.Root.position - WorkPoint(station)).sqrMagnitude;
                    if (candidate >= distance) continue;
                    distance = candidate;
                    nearest = member;
                }
                if (nearest == null) break;
                nearest.Reserved = true;
                _claimed[station] = true;
                if (nearest.Station != station)
                {
                    nearest.Station = station;
                    RouteTo(nearest, WorkPoint(station));
                }
            }

            foreach (var member in _members)
            {
                if (!member.Visible || member.Root == null) continue;
                FollowRoute(member);
                bool arrived = (member.Root.position - WorkPoint(member.Station)).sqrMagnitude < 0.0225f;
                if (member.Work != null) member.Work.SetWorking(arrived && Running(state, member.Station));
                if (arrived && member.Walker != null && member.Walker.VisualRoot != null)
                {
                    var forward = _stations[member.Station].transform.position - member.Root.position;
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 0.001f)
                        member.Walker.VisualRoot.rotation = Quaternion.RotateTowards(member.Walker.VisualRoot.rotation,
                            Quaternion.LookRotation(forward), Time.deltaTime * 300f);
                }
            }
        }

        bool Running(GameState state, int index)
        {
            if (_stations[index] == null) return false;
            var station = state.Station(_stations[index].StageId);
            return station != null && station.Owned && station.Running;
        }

        Vector3 WorkPoint(int station)
        {
            return _stations[station].transform.position + new Vector3(0f, 0f, -1.15f);
        }

        static void RouteTo(Member member, Vector3 target)
        {
            var start = member.Root.position;
            // Conveyors close the middle of each machine row. Cross at the open farm-side end.
            bool crossesLine = (start.z < -0.5f) != (target.z < -0.5f);
            member.Route = crossesLine ? new[]
            {
                new Vector3(3.35f, 0f, start.z),
                new Vector3(3.35f, 0f, target.z), target
            } : new[] { target };
            member.Waypoint = 0;
            if (member.Walker != null) member.Walker.WalkTo(member.Route[0]);
        }

        static void FollowRoute(Member member)
        {
            if (member.Route == null) return;
            var target = member.Route[member.Waypoint];
            if ((member.Root.position - target).sqrMagnitude < 0.0225f)
            {
                member.Waypoint++;
                if (member.Waypoint >= member.Route.Length) { member.Route = null; return; }
                target = member.Route[member.Waypoint];
                if (member.Walker != null) member.Walker.WalkTo(target);
            }
            if (member.Walker == null)
                member.Root.position = Vector3.MoveTowards(member.Root.position, target, 2.1f * Time.deltaTime);
            else if (!member.Walker.IsWalking)
                member.Walker.WalkTo(target);
        }
    }
}
