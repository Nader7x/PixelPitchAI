'use client';
import React from 'react';

export default function Goal({ position = [0, 0, 0], rotation = [0, 0, 0] }) {
  const width = 7.32; // meters
  const height = 2.44; // meters
  const depth = 1.5; // meters (net depth)
  const barThickness = 0.1;

  // Define how many squares you want for each net
  const netSegmentsWidth = 15; // Number of squares across the width of the back net
  const netSegmentsHeight = 7.5; // Number of squares across the height of the back net
  const sideNetSegmentsWidth = 3.75; // Number of squares across the depth of the side nets
  const sideNetSegmentsHeight = 7.5; // Number of squares across the height of the side nets
  const topNetSegmentsWidth = 15; // Number of squares across the width of the top net
  const topNetSegmentsHeight = 3.75; // Number of squares across the depth of the top net

  return (
    <group position={position} rotation={rotation}>
      {/* Crossbar */}
      <mesh castShadow receiveShadow position={[0, height / 2, 0]}>
        <boxGeometry args={[width, barThickness, barThickness]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Left Post */}
      <mesh castShadow receiveShadow position={[-width / 2, 0, 0]}>
        <boxGeometry args={[barThickness, height, barThickness]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Right Post */}
      <mesh castShadow receiveShadow position={[width / 2, 0, 0]}>
        <boxGeometry args={[barThickness, height, barThickness]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Depth Supports */}
      {/* Top Left to Back */}
      <mesh position={[-width / 2, height / 2, -depth / 2]}>
        <boxGeometry args={[barThickness, barThickness, depth]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Top Right to Back */}
      <mesh position={[width / 2, height / 2, -depth / 2]}>
        <boxGeometry args={[barThickness, barThickness, depth]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Back Left Vertical */}
      <mesh position={[-width / 2, 0, -depth]}>
        <boxGeometry args={[barThickness, height, barThickness]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Back Right Vertical */}
      <mesh position={[width / 2, 0, -depth]}>
        <boxGeometry args={[barThickness, height, barThickness]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Back Bottom Bar */}
      <mesh position={[0, -height / 2, -depth]}>
        <boxGeometry args={[width, barThickness, barThickness]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* Side Bars at Ground */}
      <mesh position={[-width / 2, -height / 2, -depth / 2]}>
        <boxGeometry args={[barThickness, barThickness, depth]} />
        <meshStandardMaterial color="white" />
      </mesh>

      <mesh position={[width / 2, -height / 2, -depth / 2]}>
        <boxGeometry args={[barThickness, barThickness, depth]} />
        <meshStandardMaterial color="white" />
      </mesh>

      {/* === NETS === */}

      {/* Back Net */}
      <mesh position={[0, 0, -depth]}>
        <planeGeometry
          args={[width, height, netSegmentsWidth, netSegmentsHeight]}
        />
        <meshBasicMaterial color="white" wireframe />
      </mesh>

      {/* Left Side Net */}
      <mesh
        rotation={[0, Math.PI / 2, 0]}
        position={[-width / 2, 0, -depth / 2]}
      >
        <planeGeometry
          args={[depth, height, sideNetSegmentsWidth, sideNetSegmentsHeight]}
        />
        <meshBasicMaterial color="white" wireframe />
      </mesh>

      {/* Right Side Net */}
      <mesh
        rotation={[0, -Math.PI / 2, 0]}
        position={[width / 2, 0, -depth / 2]}
      >
        <planeGeometry
          args={[depth, height, sideNetSegmentsWidth, sideNetSegmentsHeight]}
        />
        <meshBasicMaterial color="white" wireframe />
      </mesh>

      {/* Top Net */}
      <mesh
        rotation={[Math.PI / 2, 0, 0]}
        position={[0, height / 2, -depth / 2]}
      >
        <planeGeometry
          args={[width, depth, topNetSegmentsWidth, topNetSegmentsHeight]}
        />
        <meshBasicMaterial color="white" wireframe />
      </mesh>

      {/* Bottom Net (optional) */}
      {/* <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0, -depth / 2]}>
                <planeGeometry args={[width, depth, 40, 10]} />
                <meshBasicMaterial color="white" wireframe />
            </mesh> */}
    </group>
  );
}
