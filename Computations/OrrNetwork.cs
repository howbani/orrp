using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ORR.Dataplane;
using ORR.Dataplane.PacketRouter;
using System.IO;

namespace ORR.Computations
{
    class OrrNetwork
    {
        public List<Sensor> myNetWork;
        public Queue<Sensor> U = new Queue<Sensor>();
        List<Sensor> notNeighborToSink = new List<Sensor>();
        public double FS0 = 10000;
        double Ta;
        double T;
        int[] sign = new int[PublicParamerters.SizeofNetwork];
        //标记A节点的reverse forwarder是否都已经出队完了
        int[] label = new int[PublicParamerters.SizeofNetwork];

        public OrrNetwork(List<Sensor> myNetWork)
        {
            this.myNetWork = myNetWork;
            Ta = PublicParamerters.Periods.ActivePeriod;
            T = PublicParamerters.Periods.SleepPeriod;
            for (int i = 0; i < sign.Length; i++)
            {
                sign[i] = 0;
                label[i] = 0;
            }
        }

        public void compute()
        {
            initial();
            int nmax = FindAllNodeForwarders();
            Console.WriteLine(nmax);
        }

        public void initial()
        {
            PublicParamerters.SinkNode.EDC = 0;
            PublicParamerters.SinkNode.MyForwarders = null;
            List<Sensor> temp = new List<Sensor>();
            foreach (NeighborsTableEntry nei in PublicParamerters.SinkNode.NeighborsTable)
            {
                List<Sensor> forwarders = new List<Sensor>();
                nei.NeiNode.EDC = (1.0 / nei.NeiNode.EnergyRate) * (Ta / T);
                forwarders.Add(PublicParamerters.SinkNode);
                nei.NeiNode.MyForwarders = forwarders;
                temp.Add(nei.NeiNode);
            }
            foreach (Sensor i in myNetWork)
            {
                if (i.ID != PublicParamerters.SinkNode.ID && temp.Contains(i) == false)
                {
                    i.MyForwarders = new List<Sensor>();
                    notNeighborToSink.Add(i);
                }
            }
        }
        public int FindAllNodeForwarders()
        {
            double minCost = 10000000000;
            double t = 0;
            int nMaxRes = 3;
            for (int nmax = 5; nmax <= 10; nmax++)
            {
                foreach (Sensor i in notNeighborToSink)
                {
                    i.MyForwarders.Clear();
                    i.EDC = FS0;
                    U.Enqueue(i);
                }
                //ORR ALGO2
                while (U.Count > 0)
                {
                    Sensor node = U.Dequeue();
                    double FsOld = node.EDC;
                    CalculateMetric(node, nmax);
                    if (node.EDC < FsOld) 
                    {
                        foreach (NeighborsTableEntry nei in node.NeighborsTable) 
                        {
                            if (node.EDC < nei.NeiNode.EDC) 
                            {
                                U.Enqueue(nei.NeiNode);
                            }
                        }
                    }
                }
                //初始化sign和label
                ClearSignAndLabel();
                t = CalculateEnergyCost();
                Console.WriteLine("nMax= " + nmax + ",   " + "cost= " + t);
                if (minCost >= t) 
                {
                    minCost = t;
                    nMaxRes = nmax;
                }
            }
            foreach (Sensor i in notNeighborToSink) 
            {
                i.MyForwarders.Clear();
                i.EDC = FS0;
                U.Enqueue(i);
            }
            while (U.Count > 0) 
            {
                Sensor node = U.Dequeue();
                double FsOld = node.EDC;
                CalculateMetric(node, nMaxRes);
                if (node.EDC < FsOld) 
                {
                    foreach (NeighborsTableEntry nei in node.NeighborsTable) 
                    {
                        if (node.EDC < nei.NeiNode.EDC) 
                        {
                            U.Enqueue(nei.NeiNode);
                        }
                    }
                }
            }
            return nMaxRes;
        }
        public double CalculateEnergyCost() 
        {
            Queue<Sensor> H = new Queue<Sensor>();
            double res = 0;
            //寻找端节点
            foreach (Sensor node in myNetWork)
            {
                if (node.ID == PublicParamerters.SinkNode.ID)
                {
                    sign[node.ID] = 1;
                }
                else
                {
                    foreach (Sensor sen in node.MyForwarders)
                    {
                        if (sign[sen.ID] == 0)
                        {
                            sign[sen.ID] = 1;
                        }
                        label[sen.ID]++;
                    }
                }
            }
            //端节点入队，sink节点始终不会出现在队列里
            foreach (Sensor node in myNetWork)
            {
                if (sign[node.ID] == 0)
                {
                    H.Enqueue(node);
                }
            }
            //初始化ExPacktNum
            foreach (Sensor node in myNetWork) 
            {
                node.ExPacketNum = 0;
            }
            //计算每个节点的ExPacketNum
            CalculateNodeEnergyCostByQueue(H);
            //printSimulationResults.printExPacketNumInfo(myNetWork);
            foreach (Sensor node in myNetWork) 
            {
                if (node.ID == PublicParamerters.SinkNode.ID)
                {
                    continue;
                }
                else
                {
                    res += node.ExPacketNum;
                }
            }
            return res;
        }
        public void CalculateNodeEnergyCostByQueue(Queue<Sensor> H)
        {
            Sensor currentNode;
            int count = 0;
            while (H.Count > 0)
            {
                currentNode = H.Dequeue();
                //Console.WriteLine("sizeQueue: " + H.Count);
                //Console.WriteLine(currentNode.ID + "  " + "label: " + label[currentNode.ID] +"  "+ "Expacket: "+currentNode.ExPacketNum);
                //如果这个节点没计算完，就让它继续呆在队列里
                if (label[currentNode.ID] > 0)
                {
                    
                    count++;
                    if (count > 100000) 
                    {
                        return;
                    }
                    
                    H.Enqueue(currentNode);
                }
                else
                {
                    //预计传输数+1；计算它的forwarders节点的传输数
                    currentNode.ExPacketNum += 1;
                    foreach (Sensor forwarder in currentNode.MyForwarders)
                    {
                        //Console.WriteLine("NodeID: " + forwarder.ID + "  " + " Expacket: " + forwarder.ExPacketNum + "  " + "labelvalue: " + label[forwarder.ID]);
                        if (forwarder.ID == PublicParamerters.SinkNode.ID)
                        {
                            continue;
                        }
                        else
                        {
                            label[forwarder.ID]--;
                            forwarder.ExPacketNum += (1.0 / currentNode.MyForwarders.Count) * R(currentNode.MyForwarders.Count) * currentNode.ExPacketNum;
                            ///Console.WriteLine("NodeID: " + forwarder.ID + "  " + " Expacket: " + forwarder.ExPacketNum+"  "+"labelvalue: "+label[forwarder.ID]);
                            if (H.Contains(forwarder)==false && label[forwarder.ID] >= 0)
                            {
                                H.Enqueue(forwarder);
                            }
                        }
                    }
                }
            }
        }
        public double R(int n) 
        {
            double res = 0;
            double S = T / Ta;
            for (int i = 2; i <= S; i++) 
            {
                for (int j = 1; j <= n - 1; j++) 
                {
                    res += (j + 1) * probility1(n, S, j, i) * probility2(j, i - 1);
                }
            }
            return res + n * probility2(n, S);
        }
        public double probility2(int l, double m)
        {
            double sum = 0;
            for (int k = 1; k <= m; k++) 
            {
                sum += probility3(l, m, k);
            }
            return 1 - sum;
        }
        public double probility3(int n, double S, int m) 
        {
            double res = 0;
            int t = n;
            if (t > m) 
            {
                t = m;
            }
            for (int i = 1; i <= t; i++) 
            {
                res += Math.Pow(-1, i + 1) * CombineNumber(m - 1, i - 1) * CombineNumber(n, i) * factorial(i) * Math.Pow(1.0 / S, i) * Math.Pow(1 - ((double)i / S), n - i);
            }
            return res;
        }
        public double probility1(int n, double S, int l, double m) 
        {
            double res = CombineNumber(n, l) * CombineNumber(n - l, l) * Math.Pow((double)(m - 1) / S, l) * (1.0 / S) * Math.Pow((double)(S - m) / S, n - l - 1);
            return res;
        }
        public int CombineNumber(int n, int r) 
        {
            int res = 0;
            //res = (double)factorial(n) / (double)(factorial(r) * factorial(n - r));
            res = factorial(n) / (factorial(r) * factorial(n - r));
            return res;
        }
        public int factorial(int n) 
        {
            int res = 1;
            for (int i = n; i > 1; i--) 
            {
                res *= i;
            }
            return res;
        }
        public List<Sensor> FindNodeInWhoseForwarders(Sensor node) 
        {
            List<Sensor> Res = new List<Sensor>();
            foreach (Sensor i in myNetWork) 
            {
                if (i.ID != PublicParamerters.SinkNode.ID && i.MyForwarders.Contains(node) == true) 
                {
                    Res.Add(i);
                }
            }
            return Res;
        }
        //ORR ALGO1
        public void CalculateMetric(Sensor node, int nmax)
        {
            List<Sensor> V = new List<Sensor>();
            Sensor t;
            node.EDC = FS0;
            node.MyForwarders.Clear();
            foreach (NeighborsTableEntry nei in node.NeighborsTable)
            {
                if (nei.NeiNode.EDC < node.EDC)
                {
                    V.Add(nei.NeiNode);
                }
            }
            while (V.Count > 0 && node.MyForwarders.Count < nmax)
            {
                t = FindMinimumFsNode(V);
                V.Remove(t);
                if (t.EDC < node.EDC)
                {
                    node.MyForwarders.Add(t);
                }
                else 
                {
                    break;
                }
                double number = (double)node.MyForwarders.Count();
                double sum = 0;
                foreach (Sensor i in node.MyForwarders) 
                {
                    sum += i.EDC;
                }
                node.EDC = (1.0 / (node.EnergyRate * (number + 1))) + (sum / number);
            }
        }
        public Sensor FindMinimumFsNode(List<Sensor> V) 
        {
            Sensor res = null;
            double min = 100000;
            foreach (Sensor i in V) 
            {
                if (min > i.EDC) 
                {
                    res = i;
                    min = i.EDC;
                }
            }
            return res;
        }
        //rectify
        //clear sign
        public void ClearSignAndLabel()
        {
            for (int i = 0; i < sign.Length; i++)
            {
                sign[i] = 0;
                label[i] = 0;
            }
        }
    }
}
