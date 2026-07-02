import React, { useEffect, useState } from 'react';
import axios from 'axios';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js';
import { Line } from 'react-chartjs-2';
import { Activity, Box, Target, Zap, ShieldAlert } from 'lucide-react';
import './App.css';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

const API_BASE = 'http://localhost:5181/api/dashboard';

interface Summary {
  totalFrames: number;
  totalInferences: number;
  avgDrift: number;
  activeModels: number;
}

interface DriftMetric {
  id: string;
  modelName: string;
  iouScore: number;
  confidenceDelta: number;
  classificationCorrect: boolean;
  computedAt: string;
  latency: number;
}

function App() {
  const [summary, setSummary] = useState<Summary | null>(null);
  const [metrics, setMetrics] = useState<DriftMetric[]>([]);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const sumRes = await axios.get<Summary>(`${API_BASE}/summary`);
        setSummary(sumRes.data);

        const metRes = await axios.get<DriftMetric[]>(`${API_BASE}/drift-metrics?limit=50`);
        setMetrics(metRes.data);
      } catch (err) {
        console.error('API Error:', err);
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 2000); // Poll every 2 seconds
    return () => clearInterval(interval);
  }, []);

  const chartData = {
    labels: metrics.map((_, i) => i),
    datasets: [
      {
        label: 'IoU Score (Accuracy)',
        data: metrics.map(m => m.iouScore),
        borderColor: '#00f2fe',
        backgroundColor: 'rgba(0, 242, 254, 0.1)',
        tension: 0.4,
        fill: true,
        pointRadius: 2,
      }
    ]
  };

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { labels: { color: 'rgba(255,255,255,0.7)' } }
    },
    scales: {
      x: { display: false },
      y: { 
        min: 0, 
        max: 1,
        grid: { color: 'rgba(255,255,255,0.05)' },
        ticks: { color: 'rgba(255,255,255,0.5)' }
      }
    }
  };

  const latencyData = {
    labels: metrics.map((_, i) => i),
    datasets: [
      {
        label: 'Inference Latency (ms)',
        data: metrics.map(m => m.latency),
        borderColor: '#f43f5e',
        backgroundColor: 'rgba(244, 63, 94, 0.1)',
        tension: 0.4,
        fill: true,
        pointRadius: 2,
      }
    ]
  };

  const latencyOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { labels: { color: 'rgba(255,255,255,0.7)' } }
    },
    scales: {
      x: { display: false },
      y: { 
        min: 0, 
        max: 100,
        grid: { color: 'rgba(255,255,255,0.05)' },
        ticks: { color: 'rgba(255,255,255,0.5)' }
      }
    }
  };

  return (
    <div className="app-container">
      <header className="header">
        <h1>
          <ShieldAlert size={32} color="#00f2fe" />
          AI Model Evaluation Center
        </h1>
        <div className="status-badge">
          <div className="pulse"></div>
          SYSTEM LIVE
        </div>
      </header>

      <div className="summary-grid">
        <div className="glass-card">
          <div className="card-title"><Box size={18} /> Ingested Frames</div>
          <p className="card-value">{summary?.totalFrames.toLocaleString() ?? '...'}</p>
        </div>
        <div className="glass-card">
          <div className="card-title"><Activity size={18} /> Total Inferences</div>
          <p className="card-value">{summary?.totalInferences.toLocaleString() ?? '...'}</p>
        </div>
        <div className="glass-card">
          <div className="card-title"><Target size={18} /> Global Avg IoU</div>
          <p className="card-value">{summary?.avgDrift.toFixed(3) ?? '...'}</p>
        </div>
        <div className="glass-card">
          <div className="card-title"><Zap size={18} /> Active Models</div>
          <p className="card-value">{summary?.activeModels ?? '...'}</p>
        </div>
      </div>

      <div className="charts-grid">
        <div className="glass-card">
          <div className="card-title" style={{marginBottom: '1rem'}}>Model Accuracy Trend (IoU)</div>
          <div className="chart-container">
            <Line data={chartData} options={chartOptions} />
          </div>
        </div>
        <div className="glass-card">
          <div className="card-title" style={{marginBottom: '1rem'}}>Inference Latency Trend</div>
          <div className="chart-container">
            <Line data={latencyData} options={latencyOptions} />
          </div>
        </div>
      </div>

      <div className="glass-card">
        <div className="card-title" style={{marginBottom: '1rem'}}>Recent Drift Inferences</div>
        <div className="table-container">
          <table>
            <thead>
              <tr>
                <th>Model</th>
                <th>Time</th>
                <th>IoU Score</th>
                <th>Confidence Delta</th>
                <th>Status</th>
                <th>Latency</th>
              </tr>
            </thead>
            <tbody>
              {metrics.slice(-10).reverse().map(m => {
                const isBad = m.iouScore < 0.5;
                const isWarning = m.iouScore < 0.8;
                return (
                  <tr key={m.id}>
                    <td style={{fontWeight: 600}}>{m.modelName}</td>
                    <td style={{color: '#888'}}>{new Date(m.computedAt).toLocaleTimeString()}</td>
                    <td className={isBad ? 'drift-high' : isWarning ? 'drift-med' : 'drift-low'}>
                      {m.iouScore.toFixed(3)}
                    </td>
                    <td>{m.confidenceDelta.toFixed(3)}</td>
                    <td>
                      {m.classificationCorrect ? 
                        <span style={{color: '#10b981'}}>Matched</span> : 
                        <span style={{color: '#ef4444'}}>Missed</span>
                      }
                    </td>
                    <td>{m.latency.toFixed(1)} ms</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default App;
