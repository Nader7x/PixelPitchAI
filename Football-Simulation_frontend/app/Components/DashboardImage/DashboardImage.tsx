'use client';
import Image from 'next/image';
import { useState, useEffect } from 'react';

export default function DashboardImage() {
  const [imageIndex, setImageIndex] = useState(0);
  const images = [
    '/images/Stadium dark.png',
    '/images/Messi shooting.png',
    '/images/greenPitch.jpg',
  ];

  const titles = [
    'Welcome to Footex',
    'Track Your Favorite Teams',
    'Stay Updated with Live Matches',
  ];

  const subtitles = [
    'Your Ultimate Football Experience',
    'Follow Players, Teams and Leagues',
    'Real-time Stats and Analysis',
  ];

  useEffect(() => {
    const interval = setInterval(() => {
      setImageIndex((prevIndex) => (prevIndex + 1) % images.length);
    }, 5000);

    return () => clearInterval(interval);
  }, []);

  return (
    <div className="relative mb-8 h-[270px] w-full overflow-hidden rounded-xl shadow-lg">
      {/* Main Image */}
      <div className="relative h-full w-full">
        <Image
          src={images[imageIndex]}
          alt="Football"
          fill
          priority
          className="object-cover transition-opacity duration-1000"
        />

        {/* Gradient Overlay */}
        <div className="absolute inset-0 z-10 bg-gradient-to-r from-black/70 to-transparent"></div>

        {/* Text Content */}
        <div className="absolute inset-0 z-20 flex flex-col justify-center p-8 text-white">
          <h1 className="animate-fadeIn mb-2 text-3xl font-bold">
            {titles[imageIndex]}
          </h1>
          <p className="animate-fadeIn text-lg opacity-80">
            {subtitles[imageIndex]}
          </p>
        </div>

        {/* Indicator Dots */}
        <div className="absolute bottom-4 left-1/2 z-20 flex -translate-x-1/2 transform space-x-2">
          {images.map((_, i) => (
            <button
              key={i}
              onClick={() => setImageIndex(i)}
              className={`h-2 w-2 rounded-full transition-all duration-300 ${i === imageIndex ? 'w-4 bg-white' : 'bg-white/50'}`}
            />
          ))}
        </div>
      </div>
    </div>
  );
}
