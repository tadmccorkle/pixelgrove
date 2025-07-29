import { useEffect, useState } from "react";

interface Event {
  id: number;
  name: string;
  host: number;
  topic: string;
}

export function EventList() {
  const [events, setEvents] = useState<Event[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const refreshEvents = async () => {
    try {
      const response = await fetch("/events");
      setEvents(await response.json());
    } catch (err) {
      setEvents([]);
      alert(err);
    }
  };

  useEffect(() => {
    (async () => {
      try {
        await refreshEvents();
      } finally {
        setIsLoading(false);
      }
    })();
  }, []);

  return isLoading ? (
    <p>Loading...</p>
  ) : (
    <div>
      <button onClick={refreshEvents}>Refresh</button>
      {events.length == 0 ? (
        <p>No events...</p>
      ) : (
        <div>
          {events.map((e) => {
            return (
              <p key={e.id}>
                {e.name}: {e.topic}
              </p>
            );
          })}
        </div>
      )}
    </div>
  );
}
