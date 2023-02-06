// the inspiration: https://www.youtube.com/watch?v=p4YirERTVF0


ArrayList<Dot> dots = new ArrayList<Dot>();

int numDots = 1000;
float noiseIncr = 0.0;
float _noiseIncr = 0.001;

//ArrayList<ArrayList<DotColors>> matrix = new ArrayList<ArrayList<DotColors
HashMap<DotColors, HashMap<DotColors, Float>> matrix = new HashMap<DotColors, HashMap<DotColors, Float>>();

float[][] colorMatrix = { 
    // R    G    B    O    P
    { 1.0, 0.0, 0.0, 0.0, 0.0 },   // RED
    { 0.0, 1.0, 0.0, 0.0, 0.0 },   // GREEN
    { 0.0, 0.0, 1.0, 0.0, 0.0 },   // BLUE
    { 0.0, 0.0, 0.0, 1.0, 0.0 },   // ORANGE
    { 0.0, 0.0, 0.0, 0.0, 1.0 }    // PURPLE
};

void settings() {
  size(900, 900, P2D);
}

void setup() {
  initPoints();

  matrix.put(DotColors.RED, createDotColorsList());
  matrix.put(DotColors.GREEN, createDotColorsList());
  matrix.put(DotColors.BLUE, createDotColorsList());
  matrix.put(DotColors.ORANGE, createDotColorsList());
  matrix.put(DotColors.PURPLE, createDotColorsList());

  HashMap<DotColors, Float> a = matrix.get(DotColors.RED);
  a.put(DotColors.RED, 0.25);
  a.put(DotColors.GREEN, 0.0);
  a.put(DotColors.BLUE, 0.0);
  a.put(DotColors.ORANGE, 0.0);
  a.put(DotColors.PURPLE, -0.2);
  matrix.put(DotColors.RED, a);

  a = matrix.get(DotColors.GREEN);
  a.put(DotColors.RED, 0.0);
  a.put(DotColors.GREEN, 1.0);
  a.put(DotColors.BLUE, 0.0);
  a.put(DotColors.ORANGE, 0.2);
  a.put(DotColors.PURPLE, 0.0);
  matrix.put(DotColors.GREEN, a);

  a = matrix.get(DotColors.BLUE);
  a.put(DotColors.RED, 0.0);
  a.put(DotColors.GREEN, 0.0);
  a.put(DotColors.BLUE, 1.0);
  a.put(DotColors.ORANGE, 0.0);
  a.put(DotColors.PURPLE, 0.0);
  matrix.put(DotColors.BLUE, a);

  a = matrix.get(DotColors.ORANGE);
  a.put(DotColors.RED, 0.75);
  a.put(DotColors.GREEN, -1.0);
  a.put(DotColors.BLUE, 0.0);
  a.put(DotColors.ORANGE, 1.0);
  a.put(DotColors.PURPLE, 0.0);
  matrix.put(DotColors.ORANGE, a);

  a = matrix.get(DotColors.PURPLE);
  a.put(DotColors.RED, 0.0);
  a.put(DotColors.GREEN, 0.0);
  a.put(DotColors.BLUE, 0.0);
  a.put(DotColors.ORANGE, 0.0);
  a.put(DotColors.PURPLE, 1.0);
  matrix.put(DotColors.PURPLE, a);
}

HashMap<DotColors, Float> createDotColorsList() {
  HashMap<DotColors, Float> a = new HashMap<DotColors, Float>();
  a.put(DotColors.RED, 0.0);
  a.put(DotColors.GREEN, 0.0);
  a.put(DotColors.BLUE, 0.0);
  a.put(DotColors.ORANGE, 0.0);
  a.put(DotColors.PURPLE, 0.0);

  return a;
}

void draw() {
  if (frameCount % 60 == 0) {
    println(frameRate);
  }
  //background(0);
  fill(0, 43);
  rect(0, 0, width, height);


  float maxRange = 115;
  float minRange = 10;
  int attractionForce = 4;
  float attractionForceDiv = 250;

  for (Dot _d1 : dots) {
    for (Dot _d2 : dots) {
      if (_d1 == _d2) continue;

      if (_d1.position.dist(_d2.position) < minRange) {
        PVector d = PVector.sub(_d2.position, _d1.position);
        d = d.normalize();
        _d1.AddForce(d.mult(-(57 * attractionForce / attractionForceDiv)));
      } else if (_d1.position.dist(_d2.position) < maxRange) {
        //float colorInteraction = GetColorInteractionByDotColor(_d1.GetColor(), _d2.GetColor());
        float colorInteraction = GetColorInteractionByIndex(_d1.colorIndex, _d2.colorIndex);
        //if (_d1.GetColor() == _d2.GetColor()) {
        // move _d1 a certain
        PVector d = PVector.sub(_d2.position, _d1.position);
        d = d.normalize();
        _d1.AddForce(d.mult(colorInteraction * (attractionForce / attractionForceDiv)));
        //} else {
        //  PVector d = PVector.sub(_d2.position, _d1.position);
        //  d = d.normalize();
        //  //d.x = pow(d.x, 3.0);
        //  //d.y = pow(d.y, 3.0);
        //  _d1.AddForce(d.mult(-attractionForce / attractionForceDiv));
        //}
      }
    }
    _d1.Constrain(10, 10, width - 10, height - 10);
  }

  noiseIncr += _noiseIncr;
  for (Dot d : dots) {
    d.update(noiseIncr);
  }

  for (Dot d : dots) {
    d.draw();
  }
}

private float GetColorInteractionByDotColor(DotColors p1, DotColors p2) {
  float f = 0.0;

  HashMap<DotColors, Float> a = matrix.get(p1);

  f = a.get(p2);

  return f;
}
private float GetColorInteractionByIndex(int p1, int p2) {
  float f = 0.0;
  
  f = colorMatrix[p1][p2];

  return f;
}

void keyReleased() {
  HashMap<DotColors, Float> a;

  switch(key) {
  case '1':
    a = matrix.get(DotColors.ORANGE);
    a.put(DotColors.RED, 0.75);
    a.put(DotColors.GREEN, 0.8);
    a.put(DotColors.BLUE, 0.3);
    a.put(DotColors.ORANGE, 1.0);
    a.put(DotColors.PURPLE, 0.0);
    matrix.put(DotColors.ORANGE, a);
    break;
  case '2':
    a = matrix.get(DotColors.GREEN);
    a.put(DotColors.RED, -0.07);
    a.put(DotColors.GREEN, 1.0);
    a.put(DotColors.BLUE, 0.0);
    a.put(DotColors.ORANGE, -0.63);
    a.put(DotColors.PURPLE, 0.0);
    matrix.put(DotColors.GREEN, a);
    break;
  case '3':
    a = matrix.get(DotColors.ORANGE);
    a.put(DotColors.RED, -0.03);
    a.put(DotColors.GREEN, -0.6);
    a.put(DotColors.BLUE, 0.0);
    a.put(DotColors.ORANGE, 1.0);
    a.put(DotColors.PURPLE, 0.0);
    matrix.put(DotColors.ORANGE, a);
    break;
  case '4':

    float repelForce = -0.3;
    a = matrix.get(DotColors.RED);
    a.put(DotColors.RED, repelForce);
    a.put(DotColors.GREEN, 0.0);
    a.put(DotColors.BLUE, 0.0);
    a.put(DotColors.ORANGE, 0.0);
    a.put(DotColors.PURPLE, 0.0);
    matrix.put(DotColors.RED, a);

    a = matrix.get(DotColors.GREEN);
    a.put(DotColors.RED, 0.0);
    a.put(DotColors.GREEN, repelForce);
    a.put(DotColors.BLUE, 0.0);
    a.put(DotColors.ORANGE, 0.0);
    a.put(DotColors.PURPLE, 0.0);
    matrix.put(DotColors.GREEN, a);

    a = matrix.get(DotColors.BLUE);
    a.put(DotColors.RED, 0.0);
    a.put(DotColors.GREEN, 0.0);
    a.put(DotColors.BLUE, repelForce);
    a.put(DotColors.ORANGE, 0.0);
    a.put(DotColors.PURPLE, 0.0);
    matrix.put(DotColors.BLUE, a);

    a = matrix.get(DotColors.ORANGE);
    a.put(DotColors.RED, 0.0);
    a.put(DotColors.GREEN, 0.0);
    a.put(DotColors.BLUE, 0.0);
    a.put(DotColors.ORANGE, repelForce);
    a.put(DotColors.PURPLE, 0.0);
    matrix.put(DotColors.ORANGE, a);

    a = matrix.get(DotColors.PURPLE);
    a.put(DotColors.RED, 0.0);
    a.put(DotColors.GREEN, 0.0);
    a.put(DotColors.BLUE, 0.0);
    a.put(DotColors.ORANGE, 0.0);
    a.put(DotColors.PURPLE, repelForce);
    matrix.put(DotColors.PURPLE, a);
    break;
  }
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
      dots.add(d);
    } else if (c == 1) {
      d = new Dot(c);
      //d = new Dot(DotColors.GREEN);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      dots.add(d);
    } else if (c == 2) {
      d = new Dot(c);
      //d = new Dot(DotColors.BLUE);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      dots.add(d);
    } else if (c == 3) {
      d = new Dot(c);
      //d = new Dot(DotColors.ORANGE);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      dots.add(d);
    } else if (c == 4) {
      d = new Dot(c);
      //d = new Dot(DotColors.PURPLE);
      d.setPosition(new PVector(random(dotsCreateOuterMargin, width - dotsCreateOuterMargin), random(dotsCreateOuterMargin, height - dotsCreateOuterMargin)));
      dots.add(d);
    }
  }
}
