import numpy as np 
import matplotlib.pyplot as plt
import matplotlib.animation as animation
import serial as sr 
from collections import deque
import time


#%% -----------------------------------------------------------------------------------------------------------------------------------------------------------
# Configurações para comunicação
porta = 'COM6'  # Porta de comunicação
baundRate = 9600  # Taxa de transmissão
ser = sr.Serial(porta, baundRate,timeout=1)  # Inicializa a comunicação serial
ser.flushInput()

# Create figure for plotting
fig = plt.figure()
ax = fig.add_subplot(1, 1, 1)
xs = deque(maxlen=100)  # Store last 100 points
ys = deque(maxlen=100)  # Store last 100 points

start_time = time.time()  # Start time for x-axis
# This function is called periodically from FuncAnimation
def animate(i, xs, ys):
    # Read data from serial port
    while ser.in_waiting:
        try:
            line = ser.readline().decode('utf-8').strip()
            value = float(line)
            #print(value)  # For debugging
            
            current_time = time.time() - start_time  # Calculate elapsed time
            # Add x and y to lists
            #xs.append(i*.001)
            xs.append(current_time)  # Use elapsed time for x-axis
            ys.append(value)
        except (UnicodeDecodeError, ValueError):
            pass  # Skip invalid data
            
    # Update plot
    #ax.clear()
    if round(xs[-1]) % 5 == 0:
        ax.clear()  # Clear the plot every 5 seconds
    
    ax.plot(xs, ys, color = 'blue', label='Sensor Value')
    ax.grid(True, which='both', linestyle='--', linewidth=0.5)

    # Format plot
    plt.subplots_adjust(bottom=0.15)
    plt.title('Real-time Arduino Data')
    plt.ylabel('Value')
    plt.xlabel('Time (s)')
    ax.set_ylim([0, 255])  # Adjust based on data range

# Set up plot to call animate() function periodically
ani = animation.FuncAnimation(fig, animate, fargs=(xs, ys), interval=50)
plt.show()

# Close serial connection when plot is closed
ser.close()
print("Serial connection closed")


#%% -----------------------------------------------------------------------------------------------------------------------------------------------------------
"""# Configura a animação do gráfico

while True:
    try:
        if ser.in_waiting > 0:  # Verifica se há dados disponíveis
            linha_serial = ser.readline().decode('ascii').strip()  # Lê os dados da porta serial
            valor = float(linha_serial)  # Converte os dados lidos para float
    except:
        pass
"""