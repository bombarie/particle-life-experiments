// the inspiration: https://www.youtube.com/watch?v=p4YirERTVF0


ArrayList<Dot> dots = new ArrayList<Dot>();

int numDots = 2000;
float noiseIncr = 0.0;
float _noiseIncr = 0.001;

float dotSize = 3;

float[] dotSizesPerColor = { 3.0, 3.0, 3.0, 10.0, 1.0 };
float[] dotSizesMinMax = { 1.0, 12.0 }; 

float[][] colorMatrix = {
  // R    G    B    O    P
  { 1.0, 0.0, 0.0, 0.0, 0.0 }, // RED
  { 0.0, 1.0, 0.0, 0.0, 0.0 }, // GREEN
  { 0.0, 0.0, 1.0, 0.0, 0.0 }, // BLUE
  { 0.0, 0.0, 0.0, 1.0, 0.0 }, // ORANGE
  { 0.0, 0.0, 0.0, 0.0, 1.0 }    // PURPLE
};

void settings() {
  size(1400, 1000, P2D);
}

void setup() {
  initPoints();
}

void draw() {
  if (frameCount % 60 == 0) {
    println(frameRate);
  }
  //background(0);
  fill(0, 64);
  rect(0, 0, width, height);
  
  noiseIncr = 0.006;

  float maxRange = 90;
  float minRange = 3;
  int attractionForce = 3;
  float attractionForceDiv = 500;

  PVector d;
  float colorInteraction;
  for (Dot _d1 : dots) {
    for (Dot _d2 : dots) {
      if (_d1 == _d2) continue;

      if (_d1.position.dist(_d2.position) < (_d1.dotSize + minRange)) {
        d = PVector.sub(_d2.position, _d1.position);
        d = d.normalize();

        _d1.AddForce(d.mult(-(57 * attractionForce / attractionForceDiv)));
      } else if (_d1.position.dist(_d2.position) < maxRange) {
        colorInteraction = GetColorInteractionByIndex(_d1.colorIndex, _d2.colorIndex);

        d = PVector.sub(_d2.position, _d1.position);
        d = d.normalize();

        _d1.AddForce(d.mult(colorInteraction * (attractionForce / attractionForceDiv)));
      }
    }

    _d1.Constrain(10, 10, width - 10, height - 10);
  }

  noiseIncr += _noiseIncr;
  for (Dot _d : dots) {
    _d.update(noiseIncr);
  }

  for (Dot _d : dots) {
    _d.draw();
  }
}


private float GetColorInteractionByIndex(int p1, int p2) {
  float f = 0.0;

  f = colorMatrix[p1][p2];

  return f;
}

void keyReleased() {

  switch(key) {
  case '1':
    colorMatrix = new float[][] {
      // R    G    B    O    P
      { 1.0, 0.0, 0.0, 0.0, 0.0 }, // RED
      { 0.0, 1.0, 0.0, 0.0, 0.0 }, // GREEN
      { 0.0, 0.0, 1.0, 0.0, 0.0 }, // BLUE
      { 0.75, 0.8, 0.3, 1.0, 0.0 }, // ORANGE
      { 0.0, 0.0, 0.0, 0.0, 1.0 }    // PURPLE
    };

    //colorMatrix[4] = new float[] { 0.0, 0.0, 0.0, 1.0, 0.0 }; // ORANGE
    break;
  case '2':
    colorMatrix[3] = new float[] { 0.0, 1.0, 0.0, -0.63, 0.0 }; // GREEN

    break;
  case '3':
    colorMatrix[4] = new float[] { 0.0, -0.6, 0.0, 1.0, 0.0 }; // ORANGE


    break;

  case '4':
    float repelForce = -0.3;
    colorMatrix = new float[][] {
      // R    G    B    O    P
      { -0.3, 0.0, 0.0, 0.0, 0.0 }, // RED
      { 0.0, -0.3, 0.0, 0.0, 0.0 }, // GREEN
      { 0.0, 0.0, -0.3, 0.0, 0.0 }, // BLUE
      { 0.0, 0.0, 0.0, -0.3, 0.0 }, // ORANGE
      { 0.0, 0.0, 0.0, 0.0, -0.3 }    // PURPLE
    };
    break;

  case '5':
    colorMatrix = new float[][] {
      // R    G    B    O    P
      { 0.5, 0.4, 0.0, 0.0, 0.0 }, // RED
      { 0.0, 0.6, 0.5, 0.0, 0.0 }, // GREEN
      { -0.1, 0.0, 0.7, 0.6, 0.0 }, // BLUE
      { 0.0, 0.0, 0.0, 0.8, 0.7 }, // ORANGE
      { 0.0, 0.0, 0.0, -0.3, 1.0 }    // PURPLE
    };
    break;

  case '6':
    colorMatrix = new float[][] {
      // R    G    B    O    P
      { r(), r(), r(), r(), r() }, // RED
      { r(), r(), r(), r(), r() }, // GREEN
      { r(), r(), r(), r(), r() }, // BLUE
      { r(), r(), r(), r(), r() }, // ORANGE
      { r(), r(), r(), r(), r() } // PURPLE
    };
    break;

  case 'a':
    dotSizesPerColor[0] = random(1, 16);
    //println("color 0 random: " + dotSizesPerColor[0]);
    
    dotSizesPerColor[1] = random(1, 16);
    //println("color 1 random: " + dotSizesPerColor[1]);
    
    dotSizesPerColor[2] = random(1, 16);
    //println("color 2 random: " + dotSizesPerColor[2]);
    
    dotSizesPerColor[3] = random(1, 16);
    //println("color 3 random: " + dotSizesPerColor[3]);
    
    dotSizesPerColor[4] = random(1, 16);
    //println("color 4 random: " + dotSizesPerColor[4]);

    for (Dot _d : dots) {
      _d.setSize(dotSizesPerColor[_d.colorIndex]);
    }
    break;

  case 's':
    dotSize = random(1, 16);
    for (Dot _d : dots) {
      _d.setSize(dotSize);
    }
    break;

  case 'S':
    dotSize = 3;
    for (Dot _d : dots) {
      _d.setSize(dotSize);
    }
    break;
  }
}

float r() {
  return random(-1.0, 1.0);
}

private int dotsCreateOuterMargin = 50;
void initPoints() {
  Dot d = new Dot(DotColors.RED);

  for (int i = 0; i < numDots; i++) {
    int c = (int)random(0, 5);
    if (c == 0) {
      d = new Dot(c);
      //d = new Dot(DotColors.RED);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      d.setSize(dotSizesPerColor[c]);
      dots.add(d);
    } else if (c == 1) {
      d = new Dot(c);
      //d = new Dot(DotColors.GREEN);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      d.setSize(dotSizesPerColor[c]);
      dots.add(d);
    } else if (c == 2) {
      d = new Dot(c);
      //d = new Dot(DotColors.BLUE);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      d.setSize(dotSizesPerColor[c]);
      dots.add(d);
    } else if (c == 3) {
      d = new Dot(c);
      //d = new Dot(DotColors.ORANGE);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      d.setSize(dotSizesPerColor[c]);
      dots.add(d);
    } else if (c == 4) {
      d = new Dot(c);
      //d = new Dot(DotColors.PURPLE);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      d.setSize(dotSizesPerColor[c]);
      dots.add(d);
    }
  }
}
