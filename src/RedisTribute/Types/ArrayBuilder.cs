using System.Collections.Generic;

namespace RedisTribute.Types
{
    class ArrayBuilder
    {
        readonly Stack<RedisArray> _objectStack = new();

        RedisArray? currentArray = null;

        public IRedisObject? AddOrYield(RedisObjectPart part)
        {
            return AddOrYield(part.IsArrayStart ? new RedisArray(part.Length) : part.Value!, part.IsArrayStart);
        }

        RedisArray? YieldCurrentArray()
        {
            if (currentArray?.IsComplete == true)
            {
                while (_objectStack.Count > 0)
                {
                    currentArray = _objectStack.Pop();

                    if (!currentArray.IsComplete)
                    {
                        return null;
                    }
                }

                if (currentArray.IsComplete)
                {
                    var val = currentArray;
                    currentArray = null;
                    return val;
                }
            }

            return null;
        }

        IRedisObject? AddOrYield(IRedisObject obj, bool isArray)
        {
            if (currentArray == null)
            {
                if (isArray)
                {
                    currentArray = (RedisArray)obj;

                    return YieldCurrentArray();
                }

                return obj;
            }

            currentArray.Add(obj);

            if (isArray)
            {
                _objectStack.Push(currentArray);
                currentArray = (RedisArray)obj;
            }

            return YieldCurrentArray();
        }
    }
}