import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  Button, CircularProgress
} from '@mui/material'
import { BarChart as BarChartIcon, ArrowBack } from '@mui/icons-material'
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid
} from 'recharts'
import { api } from '../api/client'
import type { CameraConfig, DayStat } from '../types/camera'

export default function StatsPage() {
  const navigate = useNavigate()
  const [cameras, setCameras] = useState<CameraConfig[]>([])
  const [stats, setStats] = useState<Record<string, DayStat[]>>({})
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const load = async () => {
      const cams = await api.getCameras()
      setCameras(cams)
      const results = await Promise.all(
        cams.map(c => api.getCameraStats(c.name).then(data => ({ name: c.name, data })))
      )
      const map: Record<string, DayStat[]> = {}
      results.forEach(r => { map[r.name] = r.data })
      setStats(map)
      setLoading(false)
    }
    load()
  }, [])

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <BarChartIcon />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">Statistiques</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">
            Captures par jour — 30 derniers jours
          </Typography>
        </Stack>

        <Button variant="outlined" startIcon={<ArrowBack />} size="small" onClick={() => navigate('/')}>
          Retour
        </Button>

        {loading && (
          <Stack alignItems="center" spacing={2} py={8}>
            <CircularProgress />
            <Typography variant="body2" color="text.secondary">Chargement...</Typography>
          </Stack>
        )}

        {!loading && cameras.map(cam => (
          <Paper
            key={cam.name}
            elevation={0}
            sx={{
              p: 3,
              width: '100%',
              background: 'rgba(255,255,255,0.05)',
              border: '1px solid rgba(255,255,255,0.1)',
            }}
          >
            <Stack spacing={2}>
              <Typography variant="h6" fontWeight="bold">{cam.name}</Typography>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={stats[cam.name] ?? []} margin={{ top: 4, right: 8, left: -16, bottom: 48 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.08)" />
                  <XAxis
                    dataKey="date"
                    tick={{ fontSize: 11, fill: '#aaa' }}
                    angle={-45}
                    textAnchor="end"
                    interval={2}
                  />
                  <YAxis tick={{ fontSize: 11, fill: '#aaa' }} allowDecimals={false} />
                  <Tooltip
                    contentStyle={{ background: '#1e1e1e', border: '1px solid #444', borderRadius: 6 }}
                    labelStyle={{ color: '#fff' }}
                    itemStyle={{ color: '#90caf9' }}
                    formatter={(value: number) => [`${value} image(s)`, '']}
                  />
                  <Bar dataKey="count" fill="#90caf9" radius={[3, 3, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </Stack>
          </Paper>
        ))}
      </Stack>
    </Container>
  )
}
