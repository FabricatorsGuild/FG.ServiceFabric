﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Testing.Mocks.Data
{
    public class MockTransaction : ITransaction
    {
        public Task CommitAsync()
        {
            return Task.FromResult(true);
        }

        public void Abort()
        {
        }

        public long TransactionId
        {
            get { return 0L; }
        }

        public long CommitSequenceNumber
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
        }

        public Task<long> GetVisibilitySequenceNumberAsync()
        {
            return Task.FromResult(0L);
        }
    }
}