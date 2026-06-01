// components/plotters/eventPlottingHandlers.tsx
import StraightArrow from '../StraightArrow';
import CurvedArrow from '../CurvedArrow';
import { Event } from '@/types/Event';
import PlayerMarker from '../PlayerMarker';
import { Text } from '@react-three/drei';
import { useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { useState, useEffect } from 'react';
import * as THREE from 'three';
import { ReactNode } from 'react';

// Always pass homeTeam and awayTeam to event renderers!

function getMirroredPosition(
  pos: [number, number],
  team: string,
  homeTeam?: string,
  awayTeam?: string
): [number, number] {
  if (!pos) return [0, 0]; // Return default position instead of undefined/null
  if (team !== awayTeam) return pos;
  const fieldCenterX = 60;
  const fieldCenterY = 40;
  return [
    fieldCenterX - (pos[0] - fieldCenterX),
    fieldCenterY - (pos[1] - fieldCenterY),
  ];
}

// Create a simple component for match status events that behaves like other events
function MatchStatusAnimation({ event }: { event: Event }) {
  return (
    <group position={[0, 25, -45]}>
      <Text
        fontSize={6}
        color="yellow"
        anchorX="center"
        anchorY="middle"
        outlineWidth={0.3}
        outlineColor="#ff6600"
      >
        {event.event_type === 'match_start'
          ? 'Match Started'
          : event.event_type === 'first_half_end'
            ? 'HALF TIME'
            : 'Match Ended'}
      </Text>

      {/* Simple fireworks that exist as long as the event is visible */}
      <group position={[0, 0, 0]}>
        {[...Array(8)].map((_, i) => {
          const colors = [
            '#ff6b6b',
            '#4ecdc4',
            '#45b7d1',
            '#f9ca24',
            '#6c5ce7',
            '#a55eea',
          ];
          const angle = (i / 8) * Math.PI * 2;
          const distance = 15;
          return (
            <mesh
              key={i}
              position={[
                Math.cos(angle) * distance,
                25 + Math.sin(i * 0.5) * 3,
                -45 + Math.sin(angle) * distance,
              ]}
            >
              <sphereGeometry args={[0.5, 8, 8]} />
              <meshStandardMaterial
                color={colors[i % colors.length]}
                emissive={colors[i % colors.length]}
                emissiveIntensity={0.5}
              />
            </mesh>
          );
        })}
      </group>
    </group>
  );
}

export function renderMatchStatusEvent(event: Event) {
  return <MatchStatusAnimation event={event} />;
}

export function renderPassEvent(
  event: Event,
  moveBall: (pos: [number, number, number]) => void,
  homeTeam?: string,
  awayTeam?: string
) {
  // Mirror position for away team
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;

  // Mirror pass_target and shot_target if present
  const mirroredEvent = {
    ...event,
    position: [x, y] as [number, number],
    pass_target: event.pass_target
      ? getMirroredPosition(event.pass_target, event.team, homeTeam, awayTeam)
      : null,
    shot_target: event.shot_target
      ? getMirroredPosition(event.shot_target, event.team, homeTeam, awayTeam)
      : null,
  };

  // Decide color: home = blue, away = white
  let color = 'gray';
  if (event.team === homeTeam) color = 'blue';
  else if (event.team === awayTeam) color = 'white';

  return (
    <>
      <PlayerMarker
        position={[posX, 0.07, posZ]}
        color={color}
        player={event.player}
      />
      {event.height === 'Ground Pass' ? (
        <StraightArrow event={mirroredEvent} moveBall={moveBall} />
      ) : (
        <CurvedArrow event={mirroredEvent} moveBall={moveBall} />
      )}
    </>
  );
}

export function renderBallReceiptEvent(
  event: Event,
  moveBall: (pos: [number, number, number]) => void,
  homeTeam?: string,
  awayTeam?: string
) {
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;
  let color = 'gray';
  if (event.team === homeTeam) color = 'blue';
  else if (event.team === awayTeam) color = 'white';

  // Move the ball to this event's position
  moveBall([posX, 0.07, posZ]);

  return (
    <PlayerMarker
      position={[posX, 0.07, posZ]}
      color={color}
      player={event.player}
    />
  );
}

export function renderCarryEvent(
  event: Event,
  moveBall: (pos: [number, number, number]) => void,
  homeTeam?: string,
  awayTeam?: string
) {
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;
  let color = 'gray';
  if (event.team === homeTeam) color = 'blue';
  else if (event.team === awayTeam) color = 'white';

  // Move the ball to this event's position
  moveBall([posX, 0.07, posZ]);

  return (
    <PlayerMarker
      position={[posX, 0.07, posZ]}
      color={color}
      player={event.player}
    />
  );
}

export function renderPressureEvent(
  event: Event,
  homeTeam?: string,
  awayTeam?: string
) {
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;
  let color = 'gray';
  if (event.team === homeTeam) color = 'blue';
  else if (event.team === awayTeam) color = 'white';
  return (
    <PlayerMarker
      position={[posX, 0.07, posZ]}
      color={color}
      player={event.player}
    />
  );
}

export function renderThrowInPassEvent(
  event: Event,
  moveBall: (pos: [number, number, number]) => void,
  homeTeam?: string,
  awayTeam?: string
) {
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;
  let color = 'gray';
  if (event.team === homeTeam) color = 'blue';
  else if (event.team === awayTeam) color = 'white';

  // Mirror pass_target if present
  const mirroredEvent = {
    ...event,
    position: [x, y] as [number, number],
    pass_target: event.pass_target
      ? getMirroredPosition(event.pass_target, event.team, homeTeam, awayTeam)
      : null,
    startY: 4.5, // <-- Set startY above the PlayerMarker
  };

  return (
    <>
      <PlayerMarker
        position={[posX, 0.07, posZ]}
        color={color}
        player={event.player}
      />
      <CurvedArrow event={mirroredEvent} moveBall={moveBall} />
    </>
  );
}

// Add this new GoalAnimation component
function GoalAnimation({ event }: { event: Event }) {
  const ballRef = useRef<THREE.Mesh>(null);
  const goalRef = useRef<THREE.Group>(null);
  const trailRef = useRef<THREE.Group>(null);
  const [animationPhase, setAnimationPhase] = useState(0);
  const [showExplosion, setShowExplosion] = useState(false);
  const [ballScale, setBallScale] = useState(1);

  useEffect(() => {
    // Phase 1: Ball appears and starts flying (immediate)
    setAnimationPhase(1);

    // Phase 2: Ball hits goal and explodes (after 1.5 seconds)
    setTimeout(() => {
      setShowExplosion(true);
      setAnimationPhase(2);
    }, 1500);

    // Phase 3: Fade out (after 4 seconds)
    setTimeout(() => setAnimationPhase(3), 4000);
  }, []);

  // Epic ball flight animation with trail
  useFrame(({ clock }) => {
    const time = clock.getElapsedTime();

    if (ballRef.current && animationPhase === 1) {
      // Stunning curved trajectory
      const progress = Math.min((time % 3) / 1.5, 1);

      // Bezier curve for realistic shot trajectory
      const t = progress;
      const startX = -35,
        startY = 5,
        startZ = -30;
      const midX = 0,
        midY = 25,
        midZ = -40;
      const endX = 25,
        endY = 15,
        endZ = -48;

      // Quadratic bezier curve
      const x =
        (1 - t) * (1 - t) * startX + 2 * (1 - t) * t * midX + t * t * endX;
      const y =
        (1 - t) * (1 - t) * startY + 2 * (1 - t) * t * midY + t * t * endY;
      const z =
        (1 - t) * (1 - t) * startZ + 2 * (1 - t) * t * midZ + t * t * endZ;

      ballRef.current.position.set(x, y, z);

      // Realistic ball spin - multiple rotations for speed effect
      ballRef.current.rotation.x += 0.4;
      ballRef.current.rotation.y += 0.2;
      ballRef.current.rotation.z += 0.3;

      // Scale effect - ball appears to get closer
      const scaleEffect = 0.8 + progress * 0.4;
      setBallScale(scaleEffect);
      ballRef.current.scale.setScalar(scaleEffect);

      // Create dynamic trail effect
      if (trailRef.current && progress > 0.1) {
        trailRef.current.children.forEach((child, index) => {
          const trail = child as THREE.Mesh;
          const trailProgress = (progress - index * 0.05) * 2;

          if (trailProgress > 0 && trailProgress < 1) {
            // Follow ball with delay
            const tx =
              (1 - trailProgress) * (1 - trailProgress) * startX +
              2 * (1 - trailProgress) * trailProgress * midX +
              trailProgress * trailProgress * endX;
            const ty =
              (1 - trailProgress) * (1 - trailProgress) * startY +
              2 * (1 - trailProgress) * trailProgress * midY +
              trailProgress * trailProgress * endY;
            const tz =
              (1 - trailProgress) * (1 - trailProgress) * startZ +
              2 * (1 - trailProgress) * trailProgress * midZ +
              trailProgress * trailProgress * endZ;

            trail.position.set(tx, ty, tz);
            trail.scale.setScalar((1 - index * 0.15) * 0.3);
            (trail.material as THREE.MeshStandardMaterial).opacity =
              (1 - index * 0.2) * 0.8;
          } else {
            trail.scale.setScalar(0);
          }
        });
      }
    }
  });

  // Massive explosion animation
  useFrame(({ clock }) => {
    const time = clock.getElapsedTime();

    if (goalRef.current && showExplosion) {
      goalRef.current.children.forEach((child, index) => {
        if (child.type === 'Mesh') {
          const particle = child as THREE.Mesh;
          const phase = (time * 4 + index * 0.2) % 6;

          if (phase < 3) {
            // Epic explosion outward in 3D
            const angle = (index / 24) * Math.PI * 2;
            const verticalAngle = (index % 8) * Math.PI * 0.25;
            const distance = phase * 25;

            particle.position.x =
              25 + Math.cos(angle) * Math.cos(verticalAngle) * distance;
            particle.position.y =
              15 + Math.sin(verticalAngle) * distance + phase * 8;
            particle.position.z =
              -48 + Math.sin(angle) * Math.cos(verticalAngle) * distance;

            // Dynamic scaling and rotation
            const scale = (3 - phase) * 0.8;
            particle.scale.setScalar(scale);
            particle.rotation.x += 0.2;
            particle.rotation.y += 0.15;
            particle.rotation.z += 0.25;

            (particle.material as THREE.MeshStandardMaterial).opacity =
              (3 - phase) / 3;
            (
              particle.material as THREE.MeshStandardMaterial
            ).emissiveIntensity = (3 - phase) * 1.2;
          } else {
            particle.scale.setScalar(0);
          }
        }
      });
    }
  });

  return (
    <group position={[0, 0, 0]}>
      {/* STUNNING FLYING BALL - Using your excellent ball model */}
      <mesh
        ref={ballRef}
        position={[-35, 5, -30]}
        scale={[ballScale, ballScale, ballScale]}
      >
        <sphereGeometry args={[2, 32, 32]} />
        <meshStandardMaterial
          color="#ffffff"
          emissive="#ffdd44"
          emissiveIntensity={0.4}
          roughness={0.3}
          metalness={0.1}
        />

        {/* Soccer ball pattern overlay */}
        <mesh scale={[1.02, 1.02, 1.02]}>
          <sphereGeometry args={[2, 16, 16]} />
          <meshStandardMaterial
            color="#000000"
            transparent
            opacity={0.8}
            wireframe={true}
          />
        </mesh>

        {/* Glowing aura around ball */}
        <mesh scale={[1.3, 1.3, 1.3]}>
          <sphereGeometry args={[2, 16, 16]} />
          <meshStandardMaterial
            color="#ffdd44"
            transparent
            opacity={0.2}
            emissive="#ffdd44"
            emissiveIntensity={0.6}
          />
        </mesh>
      </mesh>

      {/* MOTION TRAIL EFFECT */}
      <group ref={trailRef}>
        {[...Array(8)].map((_, i) => (
          <mesh key={`trail-${i}`} position={[-35, 5, -30]}>
            <sphereGeometry args={[1.5, 16, 16]} />
            <meshStandardMaterial
              color="#ffffff"
              transparent
              opacity={0}
              emissive="#ffdd44"
              emissiveIntensity={0.8}
            />
          </mesh>
        ))}
      </group>

      {/* EPIC GOAL TEXT */}
      {showExplosion && (
        <group position={[0, 25, -45]}>
          <Text
            fontSize={10}
            color="#00ff00"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.5}
            outlineColor="#ffffff"
          >
            ⚽ GOOOAL! ⚽
          </Text>

          {/* Secondary text effect */}
          <Text
            position={[0, -8, 0]}
            fontSize={4}
            color="#ffdd44"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.2}
            outlineColor="#ff6600"
          >
            INCREDIBLE SHOT!
          </Text>
        </group>
      )}

      {/* MASSIVE EXPLOSION PARTICLES */}
      {showExplosion && (
        <group ref={goalRef} position={[0, 0, 0]}>
          {[...Array(24)].map((_, i) => {
            const colors = [
              '#ff6b6b',
              '#4ecdc4',
              '#45b7d1',
              '#f9ca24',
              '#6c5ce7',
              '#a55eea',
              '#00ff00',
              '#ffdd44',
              '#ff3838',
              '#2ecc71',
              '#3498db',
              '#f39c12',
            ];
            return (
              <mesh key={`explosion-${i}`} position={[25, 15, -48]}>
                <sphereGeometry args={[1.5, 12, 12]} />
                <meshStandardMaterial
                  color={colors[i % colors.length]}
                  transparent
                  opacity={1}
                  emissive={colors[i % colors.length]}
                  emissiveIntensity={1.5}
                  roughness={0.1}
                />
              </mesh>
            );
          })}

          {/* Additional sparkle effects */}
          {[...Array(12)].map((_, i) => (
            <mesh key={`sparkle-${i}`} position={[25, 15, -48]}>
              <octahedronGeometry args={[0.8, 2]} />
              <meshStandardMaterial
                color="#ffffff"
                transparent
                opacity={1}
                emissive="#ffffff"
                emissiveIntensity={2}
              />
            </mesh>
          ))}
        </group>
      )}

      {/* DRAMATIC LIGHTING EFFECTS */}
      <pointLight
        position={[25, 30, -40]}
        intensity={5}
        color="#ffffff"
        distance={60}
        decay={1}
      />

      <pointLight
        position={[0, 20, -30]}
        intensity={3}
        color="#ffdd44"
        distance={50}
        decay={1.2}
      />

      <pointLight
        position={[25, 15, -35]}
        intensity={4}
        color="#00ff00"
        distance={40}
        decay={1.5}
      />

      {/* Ball spotlight that follows the trajectory */}
      {ballRef.current && (
        <spotLight
          position={[
            ballRef.current.position.x,
            ballRef.current.position.y + 10,
            ballRef.current.position.z + 5,
          ]}
          target={ballRef.current}
          intensity={2}
          angle={Math.PI / 6}
          penumbra={0.5}
          color="#ffffff"
          distance={30}
          decay={2}
        />
      )}
    </group>
  );
}

// Update the renderShotEvent function
export function renderShotEvent(
  event: Event,
  moveBall: (pos: [number, number, number]) => void,
  homeTeam?: string,
  awayTeam?: string,
  onGoal?: (team: string) => void
) {
  // Mirror position for away team
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;

  // Mirror shot_target if present
  const mirroredEvent = {
    ...event,
    position: [x, y] as [number, number],
    shot_target: event.shot_target
      ? getMirroredPosition(event.shot_target, event.team, homeTeam, awayTeam)
      : null,
    startY: 0, // Start from the ground
    endY: 1.5, // End at y=2
  };

  // Place the ball at ground level at the start of the shot
  moveBall([posX, 0, posZ]);

  // Check if it's a goal and call the callback immediately (no useEffect needed)
  if (event.outcome === 'Goal' && onGoal) {
    onGoal(event.team);
  }

  // Decide color: home = blue, away = white
  let color = 'gray';
  if (event.team === homeTeam) color = 'blue';
  else if (event.team === awayTeam) color = 'white';

  // Show PlayerMarker at the event position and the shot arrow
  return (
    <>
      <PlayerMarker
        position={[posX, 0.07, posZ]}
        color={color}
        player={event.player}
      />
      <StraightArrow
        event={mirroredEvent}
        moveBall={moveBall}
        startY={0}
        endY={1.5}
      />

      {/* Shot text display - only for non-goals */}
      {event.outcome !== 'Goal' && (
        <group position={[0, 25, -45]}>
          <Text
            fontSize={6}
            color="#ff6600"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.3}
            outlineColor="#ffffff"
          >
            🥅 SHOT! 🥅
          </Text>
        </group>
      )}

      {/* JAW-DROPPING GOAL ANIMATION - replaces the simple text */}
      {event.outcome === 'Goal' && <GoalAnimation event={event} />}
    </>
  );
}

export function renderFoulEvent(
  event: Event,
  homeTeam?: string,
  awayTeam?: string
) {
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;

  // Determine which team gets the penalty (opposite of the fouling team)
  const penaltyTeam = event.team === homeTeam ? awayTeam : homeTeam;

  return (
    <>
      <mesh position={[posX, 0.07, posZ]}>
        <boxGeometry args={[1, 1, 1]} />
        <meshStandardMaterial color="yellow" />
      </mesh>

      {/* Show penalty text under scoreboard if outcome is penalty */}
      {event.outcome === 'Penalty' && (
        <Text
          position={[0, 35, -45]}
          fontSize={4}
          color="#ff4444"
          anchorX="center"
          anchorY="middle"
          outlineWidth={0.2}
          outlineColor="#ffffff"
        >
          PENALTY TO {penaltyTeam?.toUpperCase()}
        </Text>
      )}
    </>
  );
}

export function renderCardEvent(event: Event) {
  let cardColor = 'yellow';
  if (event.card === 'Red Card' || event.card === 'Second Yellow') {
    cardColor = 'red';
  } else if (event.card === 'Yellow Card') {
    cardColor = 'yellow';
  }

  // Position right under the scoreboard - same as goal text and match start
  const cardPosition: [number, number, number] = [0, 22, -45];
  const namePosition: [number, number, number] = [0, 23.5, -44];

  return (
    <group>
      {/* Card - made bigger */}
      <mesh position={cardPosition}>
        <planeGeometry args={[12, 16]} />
        <meshStandardMaterial color={cardColor} />
      </mesh>
      {/* Player name under the card - changed to white */}
      <Text
        position={namePosition}
        fontSize={2.5}
        color="white"
        anchorX="center"
        anchorY="top"
      >
        {event.player || 'Unknown Player'}
      </Text>
    </group>
  );
}

export function renderSaveEvent(
  event: Event,
  moveBall: (pos: [number, number, number]) => void,
  homeTeam?: string,
  awayTeam?: string
) {
  const [x, y] = getMirroredPosition(
    event.position,
    event.team,
    homeTeam,
    awayTeam
  );
  const posX = x - 60;
  const posZ = y - 40;
  let color = 'gray';
  if (event.team === homeTeam) color = 'blue';
  else if (event.team === awayTeam) color = 'white';

  // Move the ball to a higher position and a bit in front of the PlayerMarker
  const ballY = 3; // higher (above the marker)
  const ballZ = posZ - 2; // in front (toward the goal or camera, adjust as needed)
  moveBall([posX, ballY, ballZ]);

  return (
    <>
      <PlayerMarker
        position={[posX, 0.07, posZ]}
        color={color}
        player={event.player}
      />

      {/* Show save text for successful saves */}
      {(event.outcome === 'Saved Twice' || event.outcome === 'Success') && (
        <group position={[0, 25, -45]}>
          <Text
            fontSize={4}
            color="#00aaff"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.3}
            outlineColor="#ffffff"
          >
            🥅 Ball Saved by {event.player} in {event.team} 🥅
          </Text>
        </group>
      )}
    </>
  );
}

/**
 * Renders a reusable white board at a given position, with children inside.
 * @param position - [x, y, z] position of the board
 * @param width - width of the board
 * @param height - height of the board
 * @param children - ReactNode elements to render inside the board
 */
export function WhiteBoard({
  position = [0, 10, -45],
  width = 30,
  height = 30,
  children,
}: {
  position?: [number, number, number];
  width?: number;
  height?: number;
  children: ReactNode;
}) {
  return (
    <group position={position}>
      <mesh>
        <planeGeometry args={[width, height]} />
        <meshStandardMaterial color="white" />
      </mesh>
      <group>{children}</group>
    </group>
  );
}

// 3D Scoreboard Display Component
function ScoreboardDisplay({
  homeTeam,
  awayTeam,
  homeScore,
  awayScore,
  timer,
}: {
  homeTeam: string;
  awayTeam: string;
  homeScore: number;
  awayScore: number;
  timer: number;
}) {
  const formatTime = (time: number) => {
    const minutes = Math.floor(time / 60);
    const seconds = time % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  };

  return (
    <group position={[0, 45, -30]}>
      {/* Main background with stunning gradient effect - made wider */}
      <mesh position={[0, 0, -1]}>
        <planeGeometry args={[75, 18]} />
        <meshStandardMaterial color="#0f0f23" transparent opacity={0.95} />
      </mesh>

      {/* Glowing border effect - made wider */}
      <mesh position={[0, 0, -0.9]}>
        <planeGeometry args={[77, 20]} />
        <meshStandardMaterial color="#1a1a3e" transparent opacity={0.6} />
      </mesh>

      {/* Outer glow - made wider */}
      <mesh position={[0, 0, -0.8]}>
        <planeGeometry args={[79, 22]} />
        <meshStandardMaterial color="#2d2d5f" transparent opacity={0.3} />
      </mesh>

      {/* Home team section background - made wider */}
      <mesh position={[-27, 0, -0.5]}>
        <planeGeometry args={[25, 14]} />
        <meshStandardMaterial
          color="#0d47a1"
          transparent
          opacity={0.3}
          emissive="#1565c0"
          emissiveIntensity={0.1}
        />
      </mesh>

      {/* Away team section background - made wider */}
      <mesh position={[27, 0, -0.5]}>
        <planeGeometry args={[25, 14]} />
        <meshStandardMaterial
          color="#37474f"
          transparent
          opacity={0.3}
          emissive="#546e7a"
          emissiveIntensity={0.1}
        />
      </mesh>

      {/* Center score section with special highlight */}
      <mesh position={[0, 2, -0.7]}>
        <planeGeometry args={[18, 8]} />
        <meshStandardMaterial
          color="#1a237e"
          transparent
          opacity={0.4}
          emissive="#3f51b5"
          emissiveIntensity={0.2}
        />
      </mesh>

      {/* Home team - BLUE - moved further left */}
      <Text
        position={[-27, 2, 0]}
        fontSize={3.2}
        color="#2196f3"
        anchorX="center"
        anchorY="middle"
        outlineWidth={0.15}
        outlineColor="#0d47a1"
      >
        {homeTeam}
      </Text>

      {/* Score - stays in center with gold accent */}
      <Text
        position={[0, 2, 0]}
        fontSize={5.5}
        color="#ffd700"
        anchorX="center"
        anchorY="middle"
        outlineWidth={0.25}
        outlineColor="#ff8f00"
      >
        {homeScore} : {awayScore}
      </Text>

      {/* Away team - WHITE - moved further right */}
      <Text
        position={[27, 2, 0]}
        fontSize={3.2}
        color="#ffffff"
        anchorX="center"
        anchorY="middle"
        outlineWidth={0.15}
        outlineColor="#424242"
      >
        {awayTeam}
      </Text>

      {/* Timer with enhanced styling - moved down */}
      <Text
        position={[0, -5, 0]}
        fontSize={2.2}
        color="#e3f2fd"
        anchorX="center"
        anchorY="middle"
        outlineWidth={0.1}
        outlineColor="#1565c0"
      >
        ⏱ {formatTime(timer)}
      </Text>

      {/* Enhanced stadium lights */}
      <pointLight
        position={[0, 8, 8]}
        intensity={2}
        color="#ffffff"
        distance={40}
        decay={1.5}
      />

      <pointLight
        position={[-25, 5, 5]}
        intensity={1.5}
        color="#2196f3"
        distance={30}
        decay={2}
      />

      <pointLight
        position={[25, 5, 5]}
        intensity={1.5}
        color="#ffffff"
        distance={30}
        decay={2}
      />

      {/* Additional accent lights */}
      <pointLight
        position={[0, 2, 10]}
        intensity={1}
        color="#ffd700"
        distance={25}
        decay={2}
      />
    </group>
  );
}

export { ScoreboardDisplay };
