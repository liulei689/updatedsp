import numpy as np

# Read the data file
data = []
with open(r'C:\Users\liu\OneDrive\Desktop\横摇角速度 俯仰角速度 横摇角速度 俯仰角速度.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()
    for line in lines[1:]:  # Skip header
        parts = line.strip().split('\t')
        if len(parts) == 4:
            roll_rate = float(parts[0])
            pitch_rate = float(parts[1])
            roll_integral = float(parts[2])
            pitch_integral = float(parts[3])
            data.append((roll_rate, pitch_rate, roll_integral, pitch_integral))

data = np.array(data)

# Time step dt = 0.01 s
dt = 0.01

# Calculate cumulative integral starting from 0
roll_integral_calc = np.cumsum(data[:, 0] * dt)
pitch_integral_calc = np.cumsum(data[:, 1] * dt)

# But the given integrals start from non-zero, so perhaps adjust by initial value
# Check if given integral matches calculated + initial
initial_roll = data[0, 2] - roll_integral_calc[0]
initial_pitch = data[0, 3] - pitch_integral_calc[0]

roll_integral_adjusted = roll_integral_calc + initial_roll
pitch_integral_adjusted = pitch_integral_calc + initial_pitch

# Check if they match
roll_match = np.allclose(data[:, 2], roll_integral_adjusted, atol=1e-6)
pitch_match = np.allclose(data[:, 3], pitch_integral_adjusted, atol=1e-6)

print(f"Roll integral matches: {roll_match}")
print(f"Pitch integral matches: {pitch_match}")

if not roll_match:
    print("Roll differences:")
    for i in range(min(10, len(data))):
        print(f"i={i}: given={data[i,2]:.6f}, calc={roll_integral_adjusted[i]:.6f}, diff={data[i,2] - roll_integral_adjusted[i]:.6f}")

if not pitch_match:
    print("Pitch differences:")
    for i in range(min(10, len(data))):
        print(f"i={i}: given={data[i,3]:.6f}, calc={pitch_integral_adjusted[i]:.6f}, diff={data[i,3] - pitch_integral_adjusted[i]:.6f}")