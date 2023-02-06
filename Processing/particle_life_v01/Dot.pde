public enum DotColors {
  RED,
    GREEN,
    BLUE,
    ORANGE,
    PURPLE
}

float noiseStrength = .1;
float dotSize = 4;
class Dot {

  private DotColors _color;
  public PVector position = new PVector();
  public PVector speed = new PVector();
  public float friction = .85;
  public int colorIndex = 0;

  Dot(DotColors col) {
    _color = col;

    println("Dot >> init with color " + _color);
  }

  Dot(int colIndex) {
    colorIndex = colIndex;

    println("Dot >> init with colorIndex " + colorIndex);
  }

  public void update(float noiseIncr) {
    // add random movement
    //float noiseVal = noise(position.x / 1000.0, position.y / 1000.0);
    
    //this.position.x += (noise(position.x / 100.0 + noiseIncr) - .47) * noiseStrength;
    //this.position.y += (noise(position.y / 100.0 + noiseIncr) - .47) * noiseStrength;

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
    switch (colorIndex) {
    case 0:
      fill(255, 0, 0);
      break;
    case 1:
      fill(0, 255, 0);
      break;
    case 2:
      fill(0, 0, 255);
      break;
    case 3:
      fill(255, 127, 0);
      break;
    case 4:
      fill(255, 0, 255);
      break;
    }
  }

  private void selectColorByDotColor() {
    switch (_color) {
    case RED:
      fill(255, 0, 0);
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
