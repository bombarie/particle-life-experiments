public enum DotColors {
  RED,
    GREEN,
    BLUE,
    ORANGE,
    PURPLE
}

float noiseStrength = .1;
float dotsFrictionUpper = .96;
float dotsFrictionLower = .88;

class Dot {

  private DotColors _color;
  public PVector position = new PVector();
  public PVector speed = new PVector();
  public float friction = 0.92;
  public int colorIndex = 0;
  public float dotSize = 3.0;

  Dot(DotColors col) {
    _color = col;

    println("Dot >> init with color " + _color);
  }

  Dot(int colIndex) {
    colorIndex = colIndex;

    println("Dot >> init with colorIndex " + colorIndex);
  }

  public void setSize(float size) {
    //println(this + " >> f:setSize >> my colorIndex: " + this.colorIndex + " >> size target: " + size);

    this.dotSize = size;
    friction = constrain(map(this.dotSize, dotSizesMinMax[0], dotSizesMinMax[1], dotsFrictionUpper, dotsFrictionLower), dotsFrictionLower, dotsFrictionUpper);
  }

  public void update(float __noiseIncr) {
    // add random movement
    // float noiseVal = noise(position.x / 1000.0, position.y / 1000.0);
    
    // the noise addition doesn't apppear to do SHIT
    //float noiseFinalStrength = 1.0;
    //this.position.x += (noise(position.x / 100.0 + __noiseIncr) - .49) * noiseStrength * noiseFinalStrength;
    //this.position.y += (noise(position.y / 100.0 + __noiseIncr) - .49) * noiseStrength * noiseFinalStrength;

    position.x += speed.x;
    position.y += speed.y;

    speed = speed.mult(friction);
  }

  public void draw() {
    selectColorByIndex();
    ellipseMode(CENTER);
    noStroke();
    circle(position.x, position.y, dotSize);
  }

  public DotColors GetColor() {
    return _color;
  }

  public void AddForce(PVector v) {
    //position.add(v);
      speed.add(v);
  }
  public void AttractTo(PVector v) {
  }

  public void RepelFrom(PVector v) {
  }

  public void Constrain(int minX, int minY, int maxX, int maxY) {
    if (position.x < minX) {
      speed.x *= -1;
      position.x = minX;
    }
    if (position.y < minY) {
      speed.y *= -1;
      position.y = minY;
    }
    if (position.x > maxX) {
      speed.x *= -1;
      position.x = maxX;
    }
    if (position.y > maxY) {
      speed.y *= -1;
      position.y = maxY;
    }
  }

  private void selectColorByIndex() {
    if (colorIndex == 0) {
      fill(241, 47, 84);
    } else if (colorIndex == 1) {
      fill(19, 241, 103);
    } else if (colorIndex == 2) {
      fill(18, 145, 242);
    } else if (colorIndex == 3) {
      fill(236, 122, 10);
    } else if (colorIndex == 4) {
      fill(251, 14, 209);
    }
  }

  private void selectColorByDotColor() {
    switch (_color) {
    case RED:
      fill(234, 32, 108);
      break;
    case GREEN:
      fill(0, 255, 0);
      break;
    case BLUE:
      fill(0, 0, 255);
      break;
    case ORANGE:
      fill(255, 127, 0);
      break;
    case PURPLE:
      fill(255, 0, 255);
      break;
    }
  }

  public void setPosition(PVector pos) {
    this.position = pos;
  }
}
