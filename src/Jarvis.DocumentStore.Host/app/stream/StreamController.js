(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.stream').controller('StreamController', StreamController);

    StreamController.$inject = ['$scope', 'streamData', 'configService'];

    function StreamController($scope, streamData, configService) {
        var vm = this;
        vm.tenantId = "";
        vm.streamId = "Document_1";
        vm.search = search;
        vm.commits = [];
        vm.tenants = [];

        configService.getTenants().then(function (d) {
            console.log('Get Tenants', d);
            vm.tenants = d;
            vm.tenantId = d[0];
        });
        /* */
        function search() {
            vm.commits = [];
            streamData.getStream(vm.tenantId, vm.streamId).then(function(d) {
                angular.forEach(d.Commits, function (commit) {
                    if (!angular.isArray(commit.Headers)) {
                        var tmp = [];
                        angular.forEach(commit.Headers, function(v, k) {
                            tmp.push([k, v])
                        });

                        commit.Headers = tmp;
                    }
                });
                vm.streamId = d.AggregateId;
                vm.commits = d.Commits;
            });
        };
    }
})(window, window.angular);
