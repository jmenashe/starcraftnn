using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = SWIG.BWAPI.Unit;

namespace StarcraftNN.OrganismInterfaces
{
    public class UnitGroup : IList<Unit>
    {
        protected List<Unit> _units;

        public List<Unit> Units
        {
            get
            {
                return _units;
            }
        }
        public int HitPoints
        {
            get { return _units.Sum(x => x.getHitPoints()); }
        }

        public int MaxHitPoints
        {
            get;
            private set;
        }
        private bool? _hasShortRange;
        public bool HasShortRange
        {
            get
            {
                if (_hasShortRange == null)
                    _hasShortRange = _units.Any(x => Utils.isShortRange(x));
                return _hasShortRange.Value;
            }
        }
        private bool? _hasAir;
        public bool HasAir
        {
            get
            {
                if(_hasAir == null)
                    _hasAir = _units.Any(x => Utils.isAir(x));
                return _hasAir.Value;
            }
        }
        private bool? _hasAttackAirBonus;
        public bool HasAttackAirBonus
        {
            get
            {
                if (_hasAir == null)
                    _hasAir = _units.Any(x => Utils.hasAttackAirBonus(x));
                return _hasAir.Value;
            }
        }
        public UnitGroup() 
        {
            _units = new List<Unit>();
        }
        public UnitGroup(List<Unit> units)
        {
            _units = units;
            this.MaxHitPoints = _units.Sum(x => x.getType().maxHitPoints());
        }

        public static implicit operator UnitGroup(List<Unit> list)
        {
            return new UnitGroup(list);
        }

        public static implicit operator List<Unit>(UnitGroup group)
        {
            return group._units;
        }

        #region IList

        public int IndexOf(Unit item)
        {
            return _units.IndexOf(item);
        }

        public void Insert(int index, Unit item)
        {
            _units.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _units.RemoveAt(index);
        }

        public Unit this[int index]
        {
            get
            {
                return _units[index];
            }
            set
            {
                _units[index] = value;
            }
        }

        public void Add(Unit item)
        {
            _units.Add(item);
        }

        public void Clear()
        {
            _units.Clear();
        }

        public bool Contains(Unit item)
        {
            return _units.Contains(item);
        }

        public void CopyTo(Unit[] array, int arrayIndex)
        {
            _units.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _units.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Unit item)
        {
            return _units.Remove(item);
        }

        public IEnumerator<Unit> GetEnumerator()
        {
            return _units.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _units.GetEnumerator();
        }
        #endregion
    }
}
