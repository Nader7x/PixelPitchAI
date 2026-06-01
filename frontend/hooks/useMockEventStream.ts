import { useEffect, useState } from 'react';
import { Event } from '@/types/Event';
import rawEvents from '@/public/data/dummyEvents.json'; // ✅ This works because you're importing at build-time

const dummyEvents: Event[] = rawEvents as unknown as Event[];

export default function useMockEventStream() {
  const [streamedEvents, setStreamedEvents] = useState<Event[]>([]);

  useEffect(() => {
    let index = 0;
    const interval = setInterval(() => {
      if (index < dummyEvents.length) {
        setStreamedEvents((prev) => [...prev, dummyEvents[index]]);
        index++;
      } else {
        clearInterval(interval);
      }
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  return streamedEvents;
}
