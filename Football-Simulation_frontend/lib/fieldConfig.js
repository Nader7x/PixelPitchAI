export const FIELD_CONFIG = {
  // Real-world dimensions (meters)
  totalWidth: 150,
  totalHeight: 84.375,
  realWidth: 120, // Field width (X-axis)
  realHeight: 80, // Field height (Z-axis)
  offsetWidth: 15,
  offsetHeight: 2.1875,

  // Pixel dimensions
  pxWidth: 1536, // Field texture width
  pxHeight: 1024, // Field texture height
  screenWidth: 1920, // Total canvas width
  screenHeight: 1080, // Total canvas height,

  // Calculated values

  // get pxPerMeterX() { return this.pxWidth / this.realWidth }, // 12.8 px/m
  // get pxPerMeterZ() { return this.pxHeight / this.realHeight }, // 12.8 px/m
  // get offsetX() { return (this.screenWidth - this.pxWidth) / 2 / this.pxPerMeterX }, // 15m (192px)
  // get offsetZ() { return (this.screenHeight - this.pxHeight) / 2 / this.pxPerMeterZ } // ~1.8m (28px)
};
